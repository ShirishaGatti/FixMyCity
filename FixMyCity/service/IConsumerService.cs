using FixMyCityModel.Model;
using System;
using System.Collections.Generic;

namespace FixMyCity.Service
{
    public interface IConsumerService
    {
        Consumer GetProfile(int consumerId);
        void UpdateProfile(int consumerId, string name, string contact, DateTime? dob,
            string addressLine, int? cityId, int? wardId, string designation);
        List<City> GetCities();
        List<Ward> GetWardsByCity(int cityId);
    }
}