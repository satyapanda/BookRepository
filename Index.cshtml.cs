using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataTables.AspNet.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Treatment.Data;
using Treatment.Models;

namespace Treatment.Pages.Hospitals
{
    [Authorize(Roles = "Administrator,PmdAdmin,TPAUser")]
    public class IndexModel : PageModel
    {
        private readonly Treatment.Data.ApplicationDbContext _context;

        public IndexModel(Treatment.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Hospital> Hospital { get;set; }

        public IActionResult OnGetAsync()
        {
            //Hospital = await _context.Hospitals.ToListAsync();
            return Page();
        }

        public IActionResult OnPostHospitalDataTable(DataTables.AspNet.Core.IDataTablesRequest request)
        {
            var tableResult = _context.Hospitals.ToList();
            var filteredData = String.IsNullOrWhiteSpace(request.Search.Value)
                ? tableResult
                : tableResult.Where(_item => _item.HospitalName.ToUpper().Contains(request.Search.Value.ToUpper()));

            var dataPage = filteredData.Skip(request.Start).Take(request.Length);

            var response = DataTablesResponse.Create(request, dataPage.Count(), filteredData.Count(), dataPage);

            return new DataTablesJsonResult(response, true);

        }



    }
}
