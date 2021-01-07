using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Function
{
    public class FunctionHandler
    {        
        public async Task<(int, string)> Handle(HttpRequest request)
        {
             #region Function Setup
            var elaps = new ELAPSFunctionHandler(Environment.GetEnvironmentVariable("mongoEndpoint"));
            //Read input string
            var reader = new StreamReader(request.Body);
            var input = await reader.ReadToEndAsync();

            elaps.Function.Key = input;
            // Start timer
            elaps.StartTimer();
            _ = elaps.LogStartAsync();

            elaps.ReadFunctionCallDoc(input);
            #endregion

            var callChildren = Execute(elaps);

            if(callChildren)
                _ = elaps.CallChildren();

            #region Function Teardown

            //Stop timer
            var duration = elaps.StopTimer();
            await elaps.LogStopAsync(duration);

            #endregion

            return (200, $"Function execution {input}");
        }

        public bool Execute(ELAPSFunctionHandler elaps)
        {
            // If callChildren() is called explicitly within this function, return false to prevent call to children after Execute()
            return true;
        }
    }
}