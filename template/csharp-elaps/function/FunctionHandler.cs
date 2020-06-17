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
            _ = elaps.LogStartAsync();

            elaps.ReadFunctionCallDoc(input);
            #endregion

            var callChildren = Execute();

            if(callChildren)
                _ = elaps.CallChildren();

            #region Function Teardown

            //Stop timer
            var duration = elaps.StopTimer();
            await elaps.LogStopAsync(duration);

            #endregion

            return (200, $"Function execution {input}");
        }

        public bool Execute()
        {
            // If function should not block, use asyncronous "fire and forget" function calls using the _ = function() notation
            // If child functions are called during execution, return false
            return true;
        }
    }
}