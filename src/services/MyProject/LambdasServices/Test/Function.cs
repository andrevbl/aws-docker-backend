using System;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using MyProject.service.api.core.Common.AWS;
using MyProject.service.api.core.Common.DependencyInjection;
using MyProject.service.api.core.Domain.Model.CustomModel;
using MyProject.service.api.core.Log;
using MyProject.service.api.core.Domain.Model.Enums;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MyProject.service.api.core.Test
{
    public class Function : FunctionBase
    {
        private readonly ITestRepository testRepository;

        public Function()
        {
            Logger.Log($"Resolve dependency injection");

            var dependencyResolver = new DependencyResolver();

            testRepository = dependencyResolver.GetService<ITestRepository>();
        }

        public APIGatewayProxyResponse  FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var body = "";
            TestResponse returnErrorTest = new TestResponse();

            try
            {
                Logger.Log($"Deserialize body");
                var bodyObj = JsonConvert.DeserializeObject<core.Domain.Model.Test.TestRequest>(request.Body);

                Logger.Log($"Get body request");
                var bodyRequest = testRepository.BuildRequest(bodyObj);

                if (bodyRequest == null || bodyRequest.Count == 0)
                    return LambdaResponse<TestResponse>(success: false,
                                                          $"There was an error during the request",
                                                          StatusEnum.SystemError, OriginEnum.LambdaService, string.Empty);

                body = bodyRequest[0].Request;

                Logger.Log($"Executing Test Service | Body: {bodyRequest[0].Request}");

                var response =  testRepository.CallSOAP(bodyRequest[0]);

                if (!XmlResponseIsValid(return))
                    return LambdaResponse<TestResponse>(success: false,
                                                        $"There was an error during the connection",
                                                        StatusEnum.SystemError, OriginEnum.LambdaService, string.Empty);

                Logger.Log($"Save the response in the database");
                var servicePersistance = testRepository.SaveResult(response.ToString());

                if (servicePersistance == null || servicePersistance.Count == 0)
                    return HandleErrorResponse<TestResponse>(request, response);

                returnErrorTest.ProcessStatus = core.Domain.Model.Enums.StatusEnum.Success;

                LogMessage(context, "Proccess executed with success.");

                return CreateResponse(new { success = true, data = returnErrorTest });
            }
            catch (Exception _exception)
            {
                LogMessage(context, string.Format("Proccess failed - {0}", _exception.Message));
                return CreateResponse(new
                {
                    success = false,
                    message = $"There was a error during the proccess",
                    data = returnErrorTest
                });
            }
        }
    }
}
