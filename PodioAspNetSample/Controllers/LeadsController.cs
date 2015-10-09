using PodioAPI;
using System;
using System.Linq;
using System.Web.Mvc;
using PodioAspNetSample.Models;
using System.Web.Routing;
using PodioAspNetSample.ViewModels;
using PodioAPI.Exceptions;
using System.Collections.Generic;
using PodioAPI.Models;
using System.Threading.Tasks;
using PodioAspNetSample.Filters;

namespace PodioAspNetSample.Controllers
{
    [PodioAuthenticationFilter]
    public class LeadsController : Controller
    {
        public Podio PodioClient { get; set; }

        public async Task<ActionResult> Index()
        {
            ViewBag.message = TempData["message"];
            ViewBag.error = TempData["error"];

            var model = new LeadListingViewModel();
            var lead = new Lead();

            try
            {
                List<Contact> spaceUsers = await lead.GetUsers();
                Dictionary<int,string> statuses = await lead.GetAllStatuses();
                IEnumerable<Lead> leads = await lead.GetAllLeads();

                model.LeadOwnersOptions = new SelectList(spaceUsers, "ProfileId", "Name");
                model.StatusOptions = new SelectList(statuses, "Key", "Value");

                if (leads.Any())
                {
                    model.Leads = leads.Select(x =>
                                    new LeadView
                                    {
                                        Company = x.Company,
                                        LeadOwners = x.LeadOwners,
                                        ExpectedValue = x.ExpectedValue,
                                        Status = x.Status,
                                        NextFollowUp = x.NextFollowUp,
                                        PodioItemID = x.PodioItemID
                                    }).ToList();
                }
                    
            }
            catch (PodioException ex)
            {
                ViewBag.error = ex.Error.ErrorDescription;
            }
            
            return View(model);        
        }

        [HttpPost]
        public async Task<ActionResult> Filter(LeadListingViewModel leadListingModel)
        {
            var lead = new Lead();
            var model = new LeadListingViewModel();

            try
            {
                DateTime? nextFollowUpFromDate = leadListingModel.NextFollowUpFrom;
                DateTime? nextFollowUpToDate = leadListingModel.NextFollowUpTo;
                decimal? expectedValueFrom = leadListingModel.ExpectedValueFrom;
                decimal? expectedValueTo = leadListingModel.ExpectedValueTo;
                int? status = leadListingModel.Status;
                int? leadOwner = leadListingModel.LeadOwner;

                IEnumerable<Contact> spaceUsers = await lead.GetUsers();
                Dictionary<int, string> statuses = await lead.GetAllStatuses();
                IEnumerable<Lead> leads = await lead.GetAllLeads(nextFollowUpFromDate, nextFollowUpToDate, expectedValueFrom, expectedValueTo, status, leadOwner);

                model.LeadOwnersOptions = new SelectList(spaceUsers, "ProfileId", "Name", leadOwner);
                model.StatusOptions = new SelectList(statuses, "Key", "Value", status);

                if (leads.Any())
                {
                    model.Leads = leads.Select(x =>
                                    new LeadView
                                    {
                                        Company = x.Company,
                                        LeadOwners = x.LeadOwners,
                                        ExpectedValue = x.ExpectedValue,
                                        Status = x.Status,
                                        NextFollowUp = x.NextFollowUp,
                                        PodioItemID = x.PodioItemID
                                    });
                }
                    
            }
            catch (PodioException ex)
            {
                ViewBag.error = ex.Error.ErrorDescription;
            }
           
            return View("Index", model);
        }

        public async Task<ActionResult> Create()
        {
            var lead = new Lead();
            var model = new LeadViewModel();

            try
            {

                var spaceContactsTask = lead.GetSpaceContacts();
                var spaceUsersTask = lead.GetUsers();
                var statusesTask = lead.GetAllStatuses();

                List<Contact> spaceContacts = await spaceContactsTask;
                List<Contact> spaceUsers = await spaceUsersTask;
                Dictionary<int, string> statuses = await statusesTask;

                model.LeadContactsOptions = new SelectList(spaceContacts, "ProfileId", "Name");
                model.LeadOwnersOptions = new SelectList(spaceUsers, "ProfileId", "Name");
                model.StatusOptions = new SelectList(statuses, "Key", "Value");
            }
            catch (PodioException ex)
            {
                ViewBag.error = ex.Error.ErrorDescription;
            }

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Create(LeadViewModel leadViewModel)
        {
            Lead lead = Lead.LeadViewModelToLead(leadViewModel);

            try
            {
                int itemId = await lead.CreateLead(lead);

                if(itemId != default(int))
                    TempData["message"] = "New lead added succesfully";
            }
            catch (PodioException ex)
            {
                ViewBag.error = ex.Error.ErrorDescription;
            }

            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Edit(int id)
        {
            ViewBag.message = TempData["message"];
            ViewBag.error = TempData["error"];

            var lead = new Lead();
            var model = new LeadViewModel();

            try
            {
                var leadToEditTask = lead.GetLead(id);

                var spaceContactsTask = lead.GetSpaceContacts();
                var spaceUsersTask = lead.GetUsers();
                var statusesTask = lead.GetAllStatuses();

                List<Contact> spaceContacts = await spaceContactsTask;
                List<Contact> spaceUsers = await spaceUsersTask;
                Dictionary<int, string> statuses = await statusesTask;
                var leadToEdit = await leadToEditTask;

                IEnumerable<int> selectedLeadContacts = leadToEdit.Contacts != null ? leadToEdit.Contacts.Select(x => x.Key) : null;
                IEnumerable<int> selectedLeadOwners = leadToEdit.LeadOwners != null ? leadToEdit.LeadOwners.Select(x => x.Key) : null;
                int selectedStatus = leadToEdit.Status.Item1;

                model.PodioItemID = leadToEdit.PodioItemID;
                model.Company = leadToEdit.Company;
                model.ExpectedValue = leadToEdit.ExpectedValue;
                model.ProbabilityOfSale = leadToEdit.ProbabilityOfSale;
                model.NextFollowUp = leadToEdit.NextFollowUp;
                model.StreetAddress = leadToEdit.StreetAddress;
                model.City = leadToEdit.City;
                model.Zip = leadToEdit.Zip;
                model.State = leadToEdit.State;
                model.Country = leadToEdit.Country;

                model.LeadContactsOptions = new MultiSelectList(spaceContacts, "ProfileId", "Name", selectedLeadContacts);
                model.LeadOwnersOptions = new MultiSelectList(spaceUsers, "ProfileId", "Name", selectedLeadOwners);
                model.StatusOptions = new SelectList(statuses, "Key", "Value", selectedStatus);

            }
            catch (PodioException ex)
            {
                ViewBag.error = ex.Error.ErrorDescription;
            }
            
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(LeadViewModel leadViewModel, string command)
        {
            if(command == "Update")
            {
                Lead lead = Lead.LeadViewModelToLead(leadViewModel);
                try
                {
                    await lead.UpdateLead(lead);
                    TempData["message"] = "Lead updated successfully";
                }
                catch (PodioException ex)
                {
                    ViewBag.error = ex.Error.ErrorDescription;
                }

                return RedirectToAction("Edit", new { id = lead.PodioItemID });
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        public async Task<ActionResult> Delete(int id)
        {
            var lead = new Lead();
            try
            {
                await lead.DeleteLead(id);
                TempData["message"] = "Lead deleted successfully";
            }
            catch (PodioException ex)
            {
                TempData["error"] = ex.Error.ErrorDescription;
            }

            return RedirectToAction("Index");
        }
    }
}
