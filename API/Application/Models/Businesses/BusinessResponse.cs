using API.Models.Bussiness;

namespace API.Application.Models.Businesses
{
    public class BusinessResponse
    {
        public List<Business> Businesses { get; set; } = new();
        public int BusinessesPerPage { get; set; }
        public int BusinessesCount { get; set; }
    }
}
