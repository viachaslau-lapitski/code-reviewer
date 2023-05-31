using Newtonsoft.Json;

namespace vsl
{
    public record InputPR
    {
        public int Number { get; set; }
        public string UserName { get; set; }
        public string RepoName { get; set; }


        public bool IsValid() => Number != 0 && UserName != null && RepoName != null;
    }
}