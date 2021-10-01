using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using System.Web.Http.Results;

namespace GardeningSystem.RestAPI.Filters {
    public class CustomAuthenticationFilter : Attribute, IAuthenticationFilter {
        public bool AllowMultiple => false;

        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken) {
            HttpRequestMessage request = context.Request;
            AuthenticationHeaderValue authorization = request.Headers.Authorization;

            if (authorization == null) {
                context.ErrorResult = new AuthenticationFailureResult("Missing Authorization", request);
                return;
            }
            if (authorization.Scheme != "Bearer") {
                context.ErrorResult = new AuthenticationFailureResult("Invalid Authorization Scheme", request);
                return;
            }

            if (String.IsNullOrEmpty(authorization.Parameter)) {
                context.ErrorResult = new AuthenticationFailureResult("Missing Token", request);
                return;
            }

            bool checkToken = await ValidateTokenAsync(authorization.Parameter);
            if (!checkToken)
                context.ErrorResult = new AuthenticationFailureResult("Invalid Token", request);
            return;
        }

        private Task<bool> ValidateTokenAsync(string parameter) {
            //TODO: Validate Token

            if (parameter == "123456")
                return Task.FromResult(true);
            else
                return Task.FromResult(false);
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken) {
            if (context.Result is AuthenticationFailureResult) {
                var challenge = new AuthenticationHeaderValue[]
                {
                    new AuthenticationHeaderValue("Bearer","<token>")
                };
                context.Result = new UnauthorizedResult(challenge, context.Request);
                return Task.FromResult(context.Result);
            }
            else
                return Task.FromResult(0);
        }
    }
}
