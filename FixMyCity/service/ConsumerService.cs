using FixMyCity.Exceptions;
using FixMyCity.Repository;
using FixMyCityModel.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FixMyCity.Service
{
    public class ConsumerService : IConsumerService
    {
        private readonly IAuthRepository _authRepo;

        public ConsumerService() : this(new AuthRepository()) { }

        public ConsumerService(IAuthRepository authRepo)
        {
            _authRepo = authRepo;
        }

        public Consumer GetProfile(int consumerId)
        {
            if (consumerId <= 0)
                throw new BusinessException("You must be logged in to view your profile.", "NOT_AUTHENTICATED");

            var consumer = _authRepo.GetConsumerById(consumerId);
            if (consumer == null)
                throw new NotFoundException("Consumer profile not found.");

            return consumer;
        }

        public void UpdateProfile(int consumerId, string name, string contact, DateTime? dob,
            string addressLine, int? cityId, int? wardId, string designation)
        {
            if (consumerId <= 0)
                throw new BusinessException("You must be logged in to update your profile.", "NOT_AUTHENTICATED");

            if (!Regex.IsMatch(contact ?? "", @"^\d{10}$"))
                throw new BusinessException("Contact number must be exactly 10 digits.", "INVALID_CONTACT");

            if (cityId.HasValue && wardId.HasValue)
            {
                var wardsInCity = _authRepo.GetWardsByCity(cityId.Value);
                if (!wardsInCity.Any(w => w.WardId == wardId.Value))
                    throw new BusinessException("Selected ward does not belong to the selected city.", "WARD_CITY_MISMATCH");
            }

            _authRepo.UpdateConsumerProfile(consumerId, name, contact, dob, addressLine, cityId, wardId, designation);
        }

        public List<City> GetCities() => _authRepo.GetCities();
        public List<Ward> GetWardsByCity(int cityId) => _authRepo.GetWardsByCity(cityId);
    }
}