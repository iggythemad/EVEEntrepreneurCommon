﻿using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace EntrepreneurEsiApi.Api.SystemModels
{
    public class HttpMessageError
    {
        [J("error")] public string Error { get; set; }
        [J("error_description")] public string ErrorDescription { get; set; }
    }
}
