using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Treatment.Data;
using Treatment.Models;
using Treatment.Utility;

namespace Treatment.Pages.Hospitals
{
    [Authorize(Roles = "Administrator,PmdAdmin,TPAUser")]
    public class Create : PageModel
    {
        private readonly Treatment.Data.ApplicationDbContext _context;

        [BindProperty]
        public Hospital Hospital { get; set; }

        public HospitalModel hospitalModel { get; set; }
        PriceMdsUtility priceMdsUtility;

        public Create(Treatment.Data.ApplicationDbContext context)
        {
            _context = context;
            priceMdsUtility = new PriceMdsUtility(_context);
            hospitalModel = new HospitalModel(_context);
        }

        public IActionResult OnGet()
        {
            return Page();
        }        

        public IActionResult OnPostStateNamesList()
        {
            string stateName = string.Empty;

            MemoryStream stream = new MemoryStream();
            Request.Body.CopyTo(stream);
            stream.Position = 0;
            using (StreamReader reader = new StreamReader(stream))
            {
                string requestBody = reader.ReadToEnd();
                if (requestBody.Length > 0)
                {
                    var obj = JsonConvert.DeserializeObject<StateData>(requestBody);
                    if (obj != null)
                    {
                        stateName = obj.StateName;
                    }
                }
            }

            var stateDataList = priceMdsUtility.StateNamesBasedOnStateName(stateName).ToList();
            return new JsonResult(stateDataList);

        }

        public IActionResult OnPostCityNamesList()
        {
            string stateCode = string.Empty;
            string cityName = string.Empty;

            MemoryStream stream = new MemoryStream();
            Request.Body.CopyTo(stream);
            stream.Position = 0;
            using (StreamReader reader = new StreamReader(stream))
            {
                string requestBody = reader.ReadToEnd();
                if (requestBody.Length > 0)
                {
                    var obj = JsonConvert.DeserializeObject<StateData>(requestBody);
                    if (obj != null)
                    {
                        stateCode = obj.StateCode;
                        cityName = obj.CityName;
                    }
                }
            }
            if (!String.IsNullOrEmpty(stateCode))
            {
                var CityList = priceMdsUtility.CityNamesBasedOnStateCode(stateCode, cityName).ToList();
                return new JsonResult(CityList);
            }
            else
            {
                return new JsonResult(String.Empty);
            }


        }

        public IActionResult OnPostZipCodeFromZipSearch()
        {
            string statecode = string.Empty;
            string zipcodesearch = string.Empty;

            MemoryStream stream = new MemoryStream();
            Request.Body.CopyTo(stream);
            stream.Position = 0;
            using (StreamReader reader = new StreamReader(stream))
            {
                string requestBody = reader.ReadToEnd();
                if (requestBody.Length > 0)
                {
                    var obj = JsonConvert.DeserializeObject<StateData>(requestBody);
                    if (obj != null)
                    {
                        zipcodesearch = obj.ZipCode;
                        statecode = obj.StateCode;
                    }
                }
            }

            var zipCodeList = priceMdsUtility.ZipCodesBasedOnStateCode(zipcodesearch, statecode);
            return new JsonResult(zipCodeList);
        }

        public IActionResult OnPostFormData([FromBody]HospitalFormModel facility)
        {
            var result = hospitalModel.SaveHospitalFormModel(facility);

            if (result == 1)
            {
                return new JsonResult(new { Status = true });
                //return RedirectToPage("./Index");
            }
            else { return new JsonResult(new { Status = false }); }

        }
    }
}