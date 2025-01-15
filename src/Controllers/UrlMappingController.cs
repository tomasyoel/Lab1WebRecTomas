using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Shorten.Models;

namespace Shorten.Controllers
{
    public class UrlMappingController : Controller
    {
        private readonly ShortContext _context;

        public UrlMappingController(ShortContext context)
        {
            _context = context;
        }

        // GET: UrlMapping
        public async Task<IActionResult> Index()
        {
            return View(await _context.UrlMappings.ToListAsync());
        }

        // GET: UrlMapping/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var urlMapping = await _context.UrlMappings
                .FirstOrDefaultAsync(m => m.Id == id);
            if (urlMapping == null)
            {
                return NotFound();
            }

            return View(urlMapping);
        }

        // GET: UrlMapping/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: UrlMapping/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,OriginalUrl,ShortenedUrl")] UrlMapping urlMapping)
        {
            if (ModelState.IsValid)
            {
                _context.Add(urlMapping);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(urlMapping);
        }

        // GET: UrlMapping/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var urlMapping = await _context.UrlMappings.FindAsync(id);
            if (urlMapping == null)
            {
                return NotFound();
            }
            return View(urlMapping);
        }

        // POST: UrlMapping/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OriginalUrl,ShortenedUrl")] UrlMapping urlMapping)
        {
            if (id != urlMapping.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(urlMapping);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UrlMappingExists(urlMapping.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(urlMapping);
        }

        // GET: UrlMapping/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var urlMapping = await _context.UrlMappings
                .FirstOrDefaultAsync(m => m.Id == id);
            if (urlMapping == null)
            {
                return NotFound();
            }

            return View(urlMapping);
        }

        // POST: UrlMapping/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var urlMapping = await _context.UrlMappings.FindAsync(id);
            if (urlMapping != null)
            {
                _context.UrlMappings.Remove(urlMapping);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UrlMappingExists(int id)
        {
            return _context.UrlMappings.Any(e => e.Id == id);
        }
    }
}
