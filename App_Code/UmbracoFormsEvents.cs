using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Forms.Mvc;
using Umbraco.Forms.Web.Controllers;

namespace Recaptcha2.reCaptchaUmbracoForms
{
    public class UmbracoFormsEvents : ApplicationEventHandler
    {
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            UmbracoFormsController.FormValidate += FormValidate;
            LogHelper.Info<UmbracoFormsEvents>("ReCaptcha FormValidate Event added to Umbraco Forms.");
        }

        private void FormValidate(object sender, FormValidationEventArgs e)
        {
            LogHelper.Info<UmbracoFormsEvents>("FormValidate with ReCaptcha Running...");

            var matchingFields = e.Form.AllFields.Where(f => f.FieldType.Name.Contains("GoogleReCaptcha")).ToList();
            if (!matchingFields.Any()) return;

            var reCaptchaField = matchingFields.FirstOrDefault();

            var httpContext = HttpContext.Current;
            var secretKey = Umbraco.Forms.Core.Configuration.GetSetting("RecaptchaPrivateKey");

            if (secretKey == "")
            {
                LogHelper.Warn<UmbracoFormsEvents>("ERROR: ReCaptcha v.2 is missing the Secret Key - Please update the '/app_plugins/umbracoforms/umbracoforms.config' to include 'key=\"RecaptchaPrivateKey\"'");
            }
            else
            {
                var reCaptchaResponse = httpContext.Request["g-recaptcha-response"];

                var url = string.Format("https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}", secretKey, reCaptchaResponse);

                var isSuccess = false;
                var errorCodes = new List<string>();

                using (var client = new WebClient())
                {
                    var response = client.DownloadString(url);

                    var responseParsed = JObject.Parse(response);

                    //Get Success Status
                    JToken sucessToken;
                    var sucessFound = responseParsed.TryGetValue("success", out sucessToken);
                    if (sucessFound)
                    {
                        isSuccess = sucessToken.Value<bool>();
                    }

                    //Get Error codes
                    JToken errorsToken;
                    var errorsFound = responseParsed.TryGetValue("error-codes", out errorsToken);
                    if (errorsFound)
                    {
                        var errorsChildren = errorsToken.Children();
                        errorCodes.AddRange(errorsChildren.Select(child => child.Value<string>()));
                    }
                    else
                    {
                        errorCodes.Add("unknown-error");
                    }
                }

                if (isSuccess) return;

                var controller = sender as Controller;

                var compiledErrors = ",";

                foreach (var code in errorCodes)
                {
                    compiledErrors += ", " + code;
                }

                compiledErrors = compiledErrors.Replace(",,", "");

                //Add errors to Form Model
                var errorMsg =  string.Format("Recaptcha Verification Failed: {0}", compiledErrors);
                if (reCaptchaField != null)
                {
                    if (controller != null)
                    {
                        controller.ModelState.AddModelError(reCaptchaField.Id.ToString(), errorMsg);
                    }
                }
            }
        }
    }
}