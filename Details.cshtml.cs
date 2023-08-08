using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Treatment.Data;
using Treatment.Models;

namespace Treatment.Pages.Hospitals
{
    [Authorize(Roles = "Administrator,PmdAdmin")]
    public class DetailsModel : PageModel
    {
        private readonly Treatment.Data.ApplicationDbContext _context;

        public DetailsModel(Treatment.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public Hospital Hospital { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Hospital = await _context.Hospitals.FirstOrDefaultAsync(m => m.HospitalId == id);

            if (Hospital == null)
            {
                return NotFound();
            }
            return Page();
        }
    }
}
