using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataTables.AspNet.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Treatment.Models;
using Treatment.Utility;

namespace Treatment.Pages.Hospitals
{
    [Authorize(Roles = "Administrator,PmdAdmin")]
    public class EditModel : PageModel
    {
        private readonly Treatment.Data.ApplicationDbContext _context;
        PriceMdsUtility priceMdsUtility;
        public EditModel(Treatment.Data.ApplicationDbContext context, IHostingEnvironment hostingEnv)
        {
            _context = context;
            hospitalModel = new HospitalModel(_context);
            priceMdsUtility = new PriceMdsUtility(_context);
            _hostingEnvironment = hostingEnv;
        }

        [BindProperty]
        public Hospital Hospital { get; set; }

        [BindProperty]
        public string StateName { get; set; }
        [BindProperty]
        public string StateCode { get; set; }
        public HospitalModel hospitalModel { get; set; }

        IHostingEnvironment _hostingEnvironment;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Hospital = await _context.Hospitals.FirstOrDefaultAsync(m => m.HospitalId == id);

            var groupState = (from groupstate in _context.CityLatLong
                              where groupstate.StateId == Hospital.StateId
                              select groupstate).Distinct().FirstOrDefault();

            StateName = groupState.StateName;
            StateCode = groupState.StateCode;

            if (Hospital == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(Hospital).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HospitalExists(Hospital.HospitalId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool HospitalExists(int id)
        {
            return _context.Hospitals.Any(e => e.HospitalId == id);
        }

        public IActionResult OnPostContactInfosBasedOnHospitalId()
        {
            string HospitalID = string.Empty;

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
                        HospitalID = obj.HospitalId;
                    }
                }
            }

            var ContactinfoList = hospitalModel.ContactInfosBasedOnHospitalId(HospitalID).ToList();
            return new JsonResult(ContactinfoList);


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


        public IActionResult OnPostCptCodesForHospital(DataTables.AspNet.Core.IDataTablesRequest request)
        {
            // Nothing important here. Just creates some mock data.
            // var data = Models.SampleEntity.GetSampleData();

            var facilityid = Request.Form["HospitalID"];
            var id = facilityid[0];


            var tableResult = priceMdsUtility.CptCodesBasedonFaciltyID(id);


            // Global filtering.
            // Filter is being manually applied due to in-memmory (IEnumerable) data.
            // If you want something rather easier, check IEnumerableExtensions Sample.
            var filteredData = String.IsNullOrWhiteSpace(request.Search.Value)
                ? tableResult
                : tableResult.Where(_item => _item.CptCodes.ToUpper().Contains(request.Search.Value.ToUpper()));

            // Paging filtered data.
            // Paging is rather manual due to in-memmory (IEnumerable) data.
            var dataPage = filteredData.Skip(request.Start).Take(request.Length);

            // Response creation. To create your response you need to reference your request, to avoid
            // request/response tampering and to ensure response will be correctly created.
            var response = DataTablesResponse.Create(request, dataPage.Count(), filteredData.Count(), dataPage);

            // Easier way is to return a new 'DataTablesJsonResult', which will automatically convert your
            // response to a json-compatible content, so DataTables can read it when received.
            return new DataTablesJsonResult(response, true);


        }

        public IActionResult OnPostImport()
        {
            List<CptUploadData> Cptuploadlist = new List<CptUploadData>();

            IFormFile file = Request.Form.Files[0];

            int facilityID = 0;
            int zipCode = 0;
            var ID = Request.Form["HospitalId"][0];

            var Zipcode = Request.Form["Zipcode"][0];

            var StateCode = Request.Form["StateCode"][0];

            bool idresult = Int32.TryParse(ID, out facilityID);

            bool zipresult = Int32.TryParse(Zipcode, out zipCode);

            ClinicCptcodes Cptobj;
            List<ClinicCptcodes> cptlist = new List<ClinicCptcodes>();

            string folderName = "Upload";
            string webRootPath = _hostingEnvironment.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            StringBuilder sb = new StringBuilder();
            //bool resultActive = false;
            int ReadStatus = 0;

            if (idresult == true && zipresult == true)
            {
                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                }
                if (file.Length > 0)
                {
                    string sFileExtension = Path.GetExtension(file.FileName).ToLower();
                    ISheet sheet;
                    string fullPath = Path.Combine(newPath, file.FileName);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                        stream.Position = 0;
                        if (sFileExtension == ".xls")
                        {
                            HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
                            sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook  
                        }
                        else
                        {
                            XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                            sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook   
                        }

                        IRow headerRow = sheet.GetRow(0); //Get Header Row
                        int cellCount = headerRow.LastCellNum;
                        string HeaderCellValue = string.Empty;



                        for (int j = headerRow.FirstCellNum; j < cellCount; j++)
                        {
                            if (headerRow == null) continue;
                            if (headerRow.Cells.All(d => d.CellType == CellType.Blank)) continue;

                            switch (j)
                            {
                                case 0:
                                    HeaderCellValue = headerRow.GetCell(j).ToString().Trim();
                                    if (HeaderCellValue == "CptCodes") { }
                                    else
                                    { ReadStatus += 1; }
                                    break;
                                case 1:
                                    HeaderCellValue = headerRow.GetCell(j).ToString().Trim();
                                    if (HeaderCellValue == "CodeDescription") { }
                                    else { ReadStatus += 1; }
                                    break;
                                case 2:
                                    HeaderCellValue = headerRow.GetCell(j).ToString().Trim();
                                    if (HeaderCellValue == "FeePercentage") { }
                                    else { ReadStatus += 1; }
                                    break;

                                case 3:
                                    HeaderCellValue = headerRow.GetCell(j).ToString().Trim();
                                    if (HeaderCellValue == "Fee") { }
                                    else { ReadStatus += 1; }
                                    break;

                            }
                        }
                        if (ReadStatus == 0)
                        {
                            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
                            {
                                IRow row = sheet.GetRow(i);
                                if (row == null) continue;
                                if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;

                                var cptUploadData = new CptUploadData();

                                for (int j = row.FirstCellNum; j < cellCount; j++)
                                {
                                    switch (j)
                                    {
                                        case 0:
                                            cptUploadData.CptCodes = row.GetCell(j).ToString();
                                            break;
                                        case 1:
                                            cptUploadData.CodeDescription = row.GetCell(j).ToString();
                                            break;
                                        case 2:
                                            cptUploadData.FeePercentage = row.GetCell(j).ToString();
                                            break;
                                        case 3:
                                            cptUploadData.Fee = row.GetCell(j).ToString();
                                            break;

                                    }


                                }

                                cptUploadData.Active = true;
                                cptUploadData.ProcedureCodeCategory = null;
                                cptUploadData.CodeStatus = null;

                                Cptuploadlist.Add(cptUploadData);
                            }
                        }
                    }
                }

                bool actualResult = false;

                if (ReadStatus == 0)
                {
                    float Feetemp = 0;
                    foreach (var obj in Cptuploadlist)
                    {
                        Cptobj = new ClinicCptcodes();

                        Cptobj.Active = obj.Active;
                        Cptobj.CptCodes = obj.CptCodes;
                        Cptobj.FacilityId = facilityID;
                        Cptobj.ZipCode = zipCode;
                        Cptobj.StateCode = StateCode;


                        float.TryParse(obj.Fee, out Feetemp);
                        Cptobj.Fee = Feetemp;

                        float.TryParse(obj.FeePercentage, out Feetemp);
                        Cptobj.FeePercentage = Feetemp;

                        cptlist.Add(Cptobj);

                    }

                    _context.ClinicCptcodes.AddRange(cptlist);
                    var reslt = _context.SaveChanges();

                    var cptcodesFromDB = _context.Cptcode.ToList();

                    var GetDissimlarElements = cptlist.Where(p => !cptcodesFromDB.Any(p2 => p2.CptCodes == p.CptCodes));

                    Cptcode cptObj;
                    int index = 0;
                    CptUploadData CptupData;
                    List<Cptcode> newCptCodes = new List<Cptcode>();
                    foreach (var cliniccpt in GetDissimlarElements)
                    {
                        cptObj = new Cptcode();

                        index = Cptuploadlist.FindIndex(x => x.CptCodes == cliniccpt.CptCodes);
                        CptupData = Cptuploadlist[index];

                        cptObj.ProcedureCodeCategory = CptupData.ProcedureCodeCategory;
                        cptObj.CodeDescription = CptupData.CodeDescription;
                        cptObj.CptCodes = CptupData.CptCodes;
                        cptObj.CodeStatus = CptupData.CodeStatus;

                        newCptCodes.Add(cptObj);
                    }

                    _context.Cptcode.AddRange(newCptCodes);
                    int cptCount = _context.SaveChanges();

                    if (cptlist.Count == reslt && newCptCodes.Count == cptCount)
                    {
                        actualResult = true;
                    }


                }

                if (actualResult == true)
                {
                    return new JsonResult(new { Status = true, HeaderStatus = true, ZipCodeOrFacilityID = true });

                }
                else { return new JsonResult(new { Status = false, HeaderStatus = false, ZipCodeOrFacilityID = true }); }

            }
            else { return new JsonResult(new { Status = false, HeaderStatus = false, ZipCodeOrFacilityID = false }); }

        }

        public IActionResult OnPostFormData([FromBody]HospitalFormModel hospital)
        {
            var result = hospitalModel.SaveFacilityEditFormModel(hospital);

            if (result > 0)
            {
                return new JsonResult(new { Status = true });
            }
            else { return new JsonResult(new { Status = false }); }

        }

        public async Task<IActionResult> OnPostExport()
        {
            string sWebRootFolder = _hostingEnvironment.WebRootPath;
            string sFileName = @"CPTCodeTemplate" + DateTime.Now.ToShortTimeString() + ".xlsx";
            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
            var memory = new MemoryStream();
            using (var fs = new FileStream(Path.Combine(sWebRootFolder, sFileName), FileMode.Create, FileAccess.Write))
            {
                IWorkbook workbook;
                workbook = new XSSFWorkbook();
                ISheet excelSheet = workbook.CreateSheet("CPT_Template");
                IRow row = excelSheet.CreateRow(0);

                row.CreateCell(0).SetCellValue("CptCodes");
                row.CreateCell(1).SetCellValue("CodeDescription");
                row.CreateCell(2).SetCellValue("FeePercentage");
                row.CreateCell(3).SetCellValue("Fee");


                row = excelSheet.CreateRow(1);
                row.CreateCell(0).SetCellValue(43288);
                row.CreateCell(1).SetCellValue("Facility Description");
                row.CreateCell(2).SetCellValue(1);
                row.CreateCell(3).SetCellValue(1000);


                workbook.Write(fs);
            }
            using (var stream = new FileStream(Path.Combine(sWebRootFolder, sFileName), FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        }


    }


    public class CptUploadData
    {
        public string CptCodes { get; set; }
        public string CodeDescription { get; set; }
        public string FeePercentage { get; set; }
        public string Fee { get; set; }
        public bool Active { get; set; }
        public string ProcedureCodeCategory { get; set; }
        public string CodeStatus { get; set; }
    }

}
