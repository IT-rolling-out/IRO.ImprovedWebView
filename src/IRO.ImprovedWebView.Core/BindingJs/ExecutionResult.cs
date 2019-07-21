﻿using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IRO.ImprovedWebView.Core.BindingJs
{
    public class ExecutionResult
    {
        /// <summary>
        /// Serialized json value.
        /// </summary>
        public JToken Result { get; set; }

        public bool IsError { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static ExecutionResult FromJson(string json)
        {
            return JsonConvert.DeserializeObject<ExecutionResult>(json);
        }
    }
}
