using System.Threading;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApiCore31
{
    public class RemoveCancellationTokenActionDescriptorProvider : IActionDescriptorProvider
    {
        public int Order => 1;

        public void OnProvidersExecuting(ActionDescriptorProviderContext context)
        {
            foreach (var descriptor in context.Results)
            {
                for (int i = 0; i < descriptor.Parameters.Count; ++i)
                {
                    if (descriptor.Parameters[i].ParameterType == typeof(CancellationToken))
                    {
                        descriptor.Parameters.RemoveAt(i);
                        --i;
                    }
                }
            }
        }

        public void OnProvidersExecuted(ActionDescriptorProviderContext context)
        {
        }

    }

}