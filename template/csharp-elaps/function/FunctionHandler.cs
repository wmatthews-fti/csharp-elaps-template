using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Function
{
    public class FunctionHandler
    {
        ELAPSFunctionHandler elaps = new ELAPSFunctionHandler(Environment.GetEnvironmentVariable("mongoEndpoint"));
        public async Task<(int, string)> Handle(HttpRequest request)
        {
            #region Function Setup

            //Read input string
            var reader = new StreamReader(request.Body);
            var input = await reader.ReadToEndAsync();

            elaps.Function.Key = input;
            // Start timer
            elaps.StartTimer();
            await elaps.LogStartAsync();

            await elaps.ReadFunctionCallDoc(input);

            #endregion

            await Execute();

            await elaps.CallChildren();

            #region Function Teardown

            //Stop timer
            var duration = elaps.StopTimer();
            await elaps.LogStopAsync(duration);

            #endregion

            return (200, $"Executing function with key {input}");
        }

        public async Task<string> Execute()
        {
            return "Function executed successfully";
        }
    }
}