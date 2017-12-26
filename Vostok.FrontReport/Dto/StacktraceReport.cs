using Newtonsoft.Json;

namespace Vostok.FrontReport.Dto
{
    public class StackFrame
    {
        [JsonProperty("functionName")]
        public string FunctionName { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("lineNumber")]
        public int LineNumber { get; set; }

        [JsonProperty("columnNumber")]
        public int ColumnNumber { get; set; }
    }

    public class NameAndVersion
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class StacktraceReport : Report
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("stack")]
        public StackFrame[] Stack { get; set; }

        [JsonProperty("browser")]
        public NameAndVersion Browser { get; set; }

        [JsonProperty("os")]
        public NameAndVersion Os { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("sourceUrl")]
        public string SourceUrl { get; set; }


        // Following fields are experimental and can be disappeared in the future
        [JsonProperty("partyId")]
        public string PartyId { get; set; }

        [JsonProperty("departmentId")]
        public string DepartmentId { get; set; }

        [JsonProperty("salesPointId")]
        public string SalesPointId { get; set; }

        [JsonProperty("retailUiVersion")]
        public string RetailUiVersion { get; set; }

        [JsonProperty("appVersion")]
        public string AppVersion { get; set; }
    }
}