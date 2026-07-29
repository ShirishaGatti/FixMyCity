using FixMyCity.Exceptions;
using FixMyCity.Infrastructure;
using FixMyCity.Repository;
using FixMyCity.Service;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FixMyCity.service
{
    public class ComplaintService : IComplaintService
    {
        private readonly IComplaintRepository _complaintRepo;
        private readonly IConsumerService _consumerService;

        public ComplaintService()
        {
            _complaintRepo = new ComplaintRepository();
            _consumerService = new ConsumerService();
        }
           
        //public ComplaintService(IComplaintRepository complaintRepo,
        //                        IConsumerService consumerService)
        //{
        //    _complaintRepo = complaintRepo;
        //    _consumerService = consumerService;
        //}

        public MyComplaintsViewModel GetMyComplaints(int consumerId)
        {
            return new MyComplaintsViewModel
            {
                Complaints = _complaintRepo.GetByConsumerId(consumerId),
                Categories = _complaintRepo.GetCategories(),
                Priorities = _complaintRepo.GetPriorities(),
                Cities = _consumerService.GetCities()
            };
        }

        public int FileComplaint(FileComplaintViewModel vm, int consumerId)
        {
            if (string.IsNullOrWhiteSpace(vm.Title))
                throw new BusinessException("Title is required.");

            if (string.IsNullOrWhiteSpace(vm.Description))
                throw new BusinessException("Description is required.");

            Complaint complaint = new Complaint
            {
                Title = vm.Title,
                Description = vm.Description,
                CategoryId = vm.CategoryId,
                PriorityId = vm.PriorityId,
                RaisedBy = consumerId,
                AddressLine = vm.AddressLine,
                Landmark = vm.Landmark,
                WardId = vm.WardId,
                CityId = vm.CityId
            };

            return _complaintRepo.Create(complaint);
        }
    }
}