using System;

namespace KuaforumAPI.Application.DTOs.Auth
{
    public class AddressDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string OpenAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsDefault { get; set; }
    }

    public class CreateAddressDto
    {
        public string Title { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string OpenAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class UpdateAddressDto
    {
        public string Title { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string OpenAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
