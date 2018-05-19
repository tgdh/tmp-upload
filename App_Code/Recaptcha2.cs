using System;
using Umbraco.Forms.Core;

namespace Recaptcha2.reCaptchaUmbracoForms
{
    public class Recaptcha2 : FieldType
    {
        public Recaptcha2()
        {
            Id = new Guid("9C804AA5-D7D6-42D8-B492-2E0E196331AD");
            Name = "GoogleReCaptcha";
            Description = "Renders Google Recaptcha 2";
            Icon = "icon-eye";
            DataType = FieldDataType.String;
            SortOrder = 10;
        }
    }
}