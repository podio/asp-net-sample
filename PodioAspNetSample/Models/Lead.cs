using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using PodioAPI;
using PodioAPI.Models;
using PodioAPI.Models.Request;
using PodioAPI.Utils.ItemFields;
using PodioAPI.Utils.ApplicationFields;
using System.Threading.Tasks;
using PodioAspNetSample.ViewModels;
using PodioAspNetSample.Utils;

namespace PodioAspNetSample.Models
{
    public class Lead
    {
        #region Private Properties

        private Podio _Podio
        {
            get
            {
                return PodioConnection.GetClient();
            }
        }

        private int AppId
        {
            get
            {
                var appId = ConfigurationManager.AppSettings["AppId"];
                if (string.IsNullOrEmpty(appId))
                    throw new Exception("AppId not set. Please set AppId in Web.config");

                return int.Parse(appId.Trim());
            }
        }

        private int SpaceId
        {
            get
            {
                var spaceId = ConfigurationManager.AppSettings["SpaceId"];
                if (string.IsNullOrEmpty(spaceId))
                    throw new Exception("SpaceId not set. Please set SpaceId in Web.config");

                return int.Parse(spaceId.Trim());
            }
        }
        #endregion

        #region Public Properties

        public int PodioItemID { get; set; }
        public string Company { get; set; }
        public Dictionary<int, string> Contacts { get; set; }
        public Dictionary<int, string> LeadOwners { get; set; }
        public decimal? ExpectedValue { get; set; }
        public int? ProbabilityOfSale { get; set; }
        public Tuple<int, string> Status { get; set; }
        public DateTime? NextFollowUp { get; set; }
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Country { get; set; }
        public string OfficeAddressMap { get; set; }

        #endregion

        #region Methods
        public async Task<IEnumerable<Lead>> GetAllLeads(DateTime? nextFollowUpFrom = null, DateTime? nextFollowUpTo = null, decimal? potentialRevenueFrom = null, decimal? potentialRevenueTo = null, int? status = null, int? leadOwner = null)
        {
            List<Lead> leads = new List<Lead>();
            int appId = AppId;
            var filterOption = new FilterOptions();
            Dictionary<string, object> filters = new Dictionary<string, object>();
            
            // Filter leads based on incomig parameters
            if (status != null)
                filters.Add("status2", status);

            if (leadOwner != null)
                filters.Add("sales-contact", new int[]{leadOwner.Value});

            if (potentialRevenueFrom != null || potentialRevenueTo != null)
                filters.Add("potential-revenue", new { from = potentialRevenueFrom, to = potentialRevenueTo });

            if (nextFollowUpFrom != null || nextFollowUpTo != null)
                filters.Add("next-follow-up", new { from = nextFollowUpFrom, to = nextFollowUpTo });

            filterOption.Filters = filters;

            var filteredContent = await _Podio.ItemService.FilterItems(appId,filterOption);
            if (filteredContent.Items.Any())
            {
                //Loop through and convert the podio items collection of Lead objects
                foreach (var item in filteredContent.Items)
                {
                    var lead = new Lead();

                    lead.PodioItemID = item.ItemId;

                    var companyField = item.Field<TextItemField>("company-or-organisation");
                    lead.Company = companyField.Value;

                    var potentialRevenueField = item.Field<MoneyItemField>("potential-revenue");
                    lead.ExpectedValue = potentialRevenueField.Value;

                    var statusField = item.Field<CategoryItemField>("status2");
                    if (statusField.Options != null && statusField.Options.Any())
                        lead.Status = new Tuple<int, string>(statusField.Options.First().Id.Value, statusField.Options.First().Text);

                    var leadOwnerField = item.Field<ContactItemField>("sales-contact");
                    lead.LeadOwners = leadOwnerField.Contacts != null ? leadOwnerField.Contacts.ToDictionary(k => k.UserId.Value, v => v.Name) : null;

                    var nextFollowUpField = item.Field<DateItemField>("next-follow-up");
                    lead.NextFollowUp = nextFollowUpField.StartDate;
 
                    leads.Add(lead);
                }
            }

            return leads;
        }

        /// <summary>
        /// Get specified item from podio and convert to Lead object.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public async Task<Lead> GetLead(int itemId)
        {
            int appId = AppId;
            var item = await _Podio.ItemService.GetItemBasic(itemId);

            var lead = new Lead();

            lead.PodioItemID = item.ItemId;

            var companyField = item.Field<TextItemField>("company-or-organisation");
            lead.Company = companyField.Value;

            var leadContactsField = item.Field<ContactItemField>("contacts");
            lead.Contacts = leadContactsField.Contacts != null ? leadContactsField.Contacts.ToDictionary(k => k.ProfileId, v => v.Name) : null;

            var leadOwnerField = item.Field<ContactItemField>("sales-contact");
            lead.LeadOwners = leadOwnerField.Contacts != null ? leadOwnerField.Contacts.ToDictionary(k => k.ProfileId, v => v.Name) : null;

            var statusField = item.Field<CategoryItemField>("status2");
            if (statusField.Options != null && statusField.Options.Any())
                lead.Status = new Tuple<int, string>(statusField.Options.First().Id.Value, statusField.Options.First().Text);

            var expectedValueField = item.Field<MoneyItemField>("potential-revenue");
            lead.ExpectedValue = expectedValueField.Value;

            var probabilityOfSaleField = item.Field<ProgressItemField>("probability-of-sale");
            lead.ProbabilityOfSale = probabilityOfSaleField.Value;

            var nextFollowUpField = item.Field<DateItemField>("next-follow-up");
            lead.NextFollowUp = nextFollowUpField.StartDate;

            var streetAddressField = item.Field<TextItemField>("street-address");
            lead.StreetAddress = streetAddressField.Value;

            var cityField = item.Field<TextItemField>("city");
            lead.City = cityField.Value;

            var stateField = item.Field<TextItemField>("state-provins-or-territory");
            lead.State = stateField.Value;

            var zipField = item.Field<TextItemField>("zip-codepost-code");
            lead.Zip = zipField.Value;

            var countryField = item.Field<TextItemField>("country");
            lead.Country = countryField.Value;

            return lead;
        }

        public async Task<int> CreateLead(Lead lead)
        {
            int appId = AppId;
            Item item = LeadToPodioItem(lead);

            int itemId = await _Podio.ItemService.AddNewItem(appId, item);
            return itemId;
        }

        public async Task<bool> UpdateLead(Lead lead)
        {
            Item item = LeadToPodioItem(lead);
            item.ItemId = lead.PodioItemID;

            var revisionId = await _Podio.ItemService.UpdateItem(item);
            return revisionId.HasValue;
        }

        public System.Threading.Tasks.Task DeleteLead(int itemId)
        {
            return _Podio.ItemService.DeleteItem(itemId);
        }


        public async Task<Dictionary<int, string>> GetAllStatuses()
        {
            Application application = await _Podio.ApplicationService.GetApp(AppId);

            var statusField = application.Field<CategoryApplicationField>("status2");
            return statusField.Options.ToDictionary(x => x.Id.Value, y => y.Text);
        }

        public Task<List<Contact>> GetSpaceContacts()
        {
            return _Podio.ContactService.GetSpaceContacts(spaceId: SpaceId);
        }

        public async Task<List<Contact>> GetUsers()
        {
            return await _Podio.ContactService.GetSpaceContacts(spaceId: SpaceId);
        }

        /// <summary>
        /// Convert Lead object to PodioAPI.Item object
        /// </summary>
        /// <param name="lead"></param>
        /// <returns></returns>
        private Item LeadToPodioItem(Lead lead)
        {
            var item = new Item();

            var companyField = item.Field<TextItemField>("company-or-organisation");
            if (!string.IsNullOrEmpty(lead.Company))
                companyField.Value = lead.Company;

            var leadContactsField = item.Field<ContactItemField>("contacts");
            if (lead.Contacts != null)
                leadContactsField.ContactIds = lead.Contacts.Select(x => x.Key);

            var leadOwnerField = item.Field<ContactItemField>("sales-contact");
            if (lead.LeadOwners != null)
                leadOwnerField.ContactIds = lead.LeadOwners.Select(x => x.Key);

            var statusField = item.Field<CategoryItemField>("status2");
            if (lead.Status.Item1 != default(int))
                statusField.OptionIds = new List<int> { lead.Status.Item1 };

            var expectedValueField = item.Field<MoneyItemField>("potential-revenue");
            if (lead.ExpectedValue.HasValue)
            {
                expectedValueField.Currency = "USD";
                expectedValueField.Value = lead.ExpectedValue;
            }
            
            var probabilityOfSaleField = item.Field<ProgressItemField>("probability-of-sale");
            if (lead.ProbabilityOfSale.HasValue)
                probabilityOfSaleField.Value = lead.ProbabilityOfSale;

            var nextFollowUpField = item.Field<DateItemField>("next-follow-up");
            if(lead.NextFollowUp.HasValue)
                nextFollowUpField.Start = lead.NextFollowUp;

            var streetAddressField = item.Field<TextItemField>("street-address");
            if (!string.IsNullOrEmpty(lead.StreetAddress))
                streetAddressField.Value = lead.StreetAddress;

            var cityField = item.Field<TextItemField>("city");
            if (!string.IsNullOrEmpty(lead.City))
                cityField.Value = lead.City;

            var stateField = item.Field<TextItemField>("state-provins-or-territory");
            if (!string.IsNullOrEmpty(lead.State))
                stateField.Value = lead.State;

            var zipField = item.Field<TextItemField>("zip-codepost-code");
            if (!string.IsNullOrEmpty(lead.Zip))
                zipField.Value = lead.Zip;

            var countryField = item.Field<TextItemField>("country");
            if(!string.IsNullOrEmpty(lead.Country))
                countryField.Value = lead.Country;

            return item;
        }

        /// <summary>
        /// Convert LeadViewModel to Lead object
        /// </summary>
        /// <param name="leadViewModel"></param>
        /// <returns></returns>
        public static Lead LeadViewModelToLead(LeadViewModel leadViewModel)
        {
            var lead = new Lead();

            lead.PodioItemID = leadViewModel.PodioItemID;
            lead.Company = leadViewModel.Company;
            lead.ExpectedValue = leadViewModel.ExpectedValue;
            lead.ProbabilityOfSale = leadViewModel.ProbabilityOfSale;
            lead.Status = new Tuple<int, string>(leadViewModel.Status, "");
            lead.NextFollowUp = leadViewModel.NextFollowUp;
            lead.StreetAddress = leadViewModel.StreetAddress;
            lead.City = leadViewModel.City;
            lead.State = leadViewModel.State;
            lead.Zip = leadViewModel.Zip;
            lead.Country = leadViewModel.Country;

            if (leadViewModel.LeadContacts != null && leadViewModel.LeadContacts.Any())
                lead.Contacts = leadViewModel.LeadContacts.ToDictionary(k => k.Value, v => v.Value.ToString());

            if (leadViewModel.LeadOwners != null && leadViewModel.LeadOwners.Any())
                lead.LeadOwners = leadViewModel.LeadOwners.ToDictionary(k => k.Value, v => v.Value.ToString());

            return lead;
        }

        #endregion
    }
}