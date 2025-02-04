using API.Models.Business;

namespace API.Models.Bussiness
{
    public class Business
    {
        public bool Active { get; init; }
        public int Id { get; init; }
        public Location Location { get; init; } = new();
        public string Name { get; init; } = string.Empty;
        public string? OfficialName { get; init; }
        public string Phone { get; init; } = string.Empty;
        public bool PosEnabled { get; init; }
        public string Status { get; init; } = string.Empty;
    }
}
