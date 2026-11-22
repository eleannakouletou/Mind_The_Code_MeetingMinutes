using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MeetingMinutes.Data;
using MeetingMinutes.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace MeetingMinutes.Controllers
{
    [Authorize]
    [AutoValidateAntiforgeryToken]
    public class MeetingItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly IWebHostEnvironment _environment;

        public MeetingItemsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: MeetingItems
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.MeetingItems.Include(m => m.Meeting).Include(m => m.RiskLevel);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: MeetingItems/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var meetingItem = await _context.MeetingItems
                .Include(m => m.Meeting)
                .Include(m => m.RiskLevel)
                .FirstOrDefaultAsync(m => m.MeetingItemID == id);
            if (meetingItem == null)
            {
                return NotFound();
            }

            return View(meetingItem);
        }

        // GET: MeetingItems/Create
        public IActionResult Create(int meetingid)
        {
            ViewData["Meetingid"] = new SelectList(_context.Meetings, "MeetingID", "Title", meetingid);
            ViewData["RiskLevelid"] = new SelectList(_context.RiskLevels, "RiskLevelID", "Name");
            return View();
        }

        // POST: MeetingItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MeetingItemID,Description,Deadline,AssignedTo,RequestedBy,ChangeRequested,VisibleInMinutes,Meetingid,RiskLevelid")] MeetingItem meetingItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(meetingItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Meetingid"] = new SelectList(_context.Meetings, "MeetingID", "Title", meetingItem.Meetingid);
            ViewData["RiskLevelid"] = new SelectList(_context.RiskLevels, "RiskLevelID", "Name", meetingItem.RiskLevelid);
            return View(meetingItem);
        }

        // GET: MeetingItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var meetingItem = await _context.MeetingItems.FindAsync(id);
            if (meetingItem == null)
            {
                return NotFound();
            }
            ViewData["Meetingid"] = new SelectList(_context.Meetings, "MeetingID", "Title", meetingItem.Meetingid);
            ViewData["RiskLevelid"] = new SelectList(_context.RiskLevels, "RiskLevelID", "Name", meetingItem.RiskLevelid);
            return View(meetingItem);
        }

        // POST: MeetingItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MeetingItemID,Description,Deadline,AssignedTo,RequestedBy,ChangeRequested,VisibleInMinutes,Meetingid,RiskLevelid")] MeetingItem meetingItem)
        {
            if (id != meetingItem.MeetingItemID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(meetingItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MeetingItemExists(meetingItem.MeetingItemID))
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
            ViewData["Meetingid"] = new SelectList(_context.Meetings, "MeetingID", "Title", meetingItem.Meetingid);
            ViewData["RiskLevelid"] = new SelectList(_context.RiskLevels, "RiskLevelID", "Name", meetingItem.RiskLevelid);
            return View(meetingItem);
        }

        // Builds the master/detail "Main" view. Returns NotFound when the meeting does not exist
        // (previously every action threw a NullReferenceException in that case).
        private IActionResult MasterDetail(int meetingId, MeetingItem selectedItem, DataDisplayModes mode)
        {
            var meeting = _context.Meetings.Find(meetingId);
            if (meeting == null)
            {
                return NotFound();
            }

            var model = new MasterDetailViewModel
            {
                Meetings = _context.Meetings.ToList(),
                SelectedMeeting = meeting,
                SelectedMeetingItem = selectedItem,
                DataEntryTarget = DataEntryTargets.MeetingItems,
                DataDisplayMode = mode
            };
            _context.Entry(meeting).Collection(m => m.MeetingItems).Load();

            ViewBag.RiskLevels = new SelectList(_context.RiskLevels, "RiskLevelID", "Name", selectedItem?.RiskLevelid);
            ViewBag.RiskLevel = selectedItem == null
                ? null
                : _context.RiskLevels.Where(l => l.RiskLevelID == selectedItem.RiskLevelid).Select(l => l.Name).FirstOrDefault();

            return View("Main", model);
        }

        public IActionResult ListItems(int meetingId)
        {
            return MasterDetail(meetingId, null, DataDisplayModes.Read);
        }

        [HttpPost]
        public IActionResult List(int meetingId)
        {
            return MasterDetail(meetingId, null, DataDisplayModes.Read);
        }

        [HttpPost]
        public IActionResult Select(int meetingId, int meetingItemId)
        {
            var item = _context.MeetingItems.Find(meetingItemId);
            if (item == null)
            {
                return NotFound();
            }
            return MasterDetail(meetingId, item, DataDisplayModes.Read);
        }

        [HttpPost]
        public IActionResult InsertEntry(int meetingId)
        {
            var participants = _context.MeetingParticipants
                .Where(m => m.MeetingId == meetingId)
                .Select(m => m.User)
                .ToList();
            participants.Insert(0, new ApplicationUser { Id = "", UserName = "select" });
            ViewBag.listofparticipants = participants;

            return MasterDetail(meetingId, null, DataDisplayModes.Insert);
        }

        [HttpPost]
        public IActionResult InsertSave([Bind("Description,Deadline,AssignedTo,RequestedBy,ChangeRequested,VisibleInMinutes,Meetingid,RiskLevelid,Files")] MeetingItem item)
        {
            if (!ModelState.IsValid || !_context.Meetings.Any(m => m.MeetingID == item.Meetingid))
            {
                return MasterDetail(item.Meetingid, null, DataDisplayModes.Insert);
            }

            if (item.Files != null)
            {
                var formfile = item.Files.FirstOrDefault(f => f.Length > 0);
                if (formfile != null)
                {
                    // Path.GetFileName strips any directory part sent by the client (path traversal).
                    var safeName = Path.GetFileName(formfile.FileName);
                    var storedName = $"{Guid.NewGuid()}_{safeName}";
                    var folder = Path.Combine(_environment.WebRootPath, "files");
                    Directory.CreateDirectory(folder);

                    using (var fileStream = new FileStream(Path.Combine(folder, storedName), FileMode.Create))
                    {
                        formfile.CopyTo(fileStream);
                    }
                    item.FileName = safeName;
                    item.FileType = formfile.ContentType;
                    item.FileAttachment = "/files/" + storedName; // relative URL, not the server's disk path
                }
            }

            _context.MeetingItems.Add(item);
            _context.SaveChanges();

            return MasterDetail(item.Meetingid, item, DataDisplayModes.Read);
        }

        [HttpPost]
        public IActionResult UpdateEntry(int meetingId, int meetingItemId)
        {
            var item = _context.MeetingItems.Find(meetingItemId);
            if (item == null)
            {
                return NotFound();
            }
            return MasterDetail(meetingId, item, DataDisplayModes.Update);
        }

        [HttpPost]
        public IActionResult UpdateSave([Bind("MeetingItemID,Description,Deadline,AssignedTo,RequestedBy,ChangeRequested,VisibleInMinutes,Meetingid,RiskLevelid")] MeetingItem item)
        {
            var existing = _context.MeetingItems.Find(item.MeetingItemID);
            if (existing == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return MasterDetail(existing.Meetingid, item, DataDisplayModes.Update);
            }

            // Copy only the editable fields. The old code did _context.Update(item) with the posted
            // object, which overwrote FileAttachment/FileName/FileType with NULL on every edit.
            existing.Description = item.Description;
            existing.Deadline = item.Deadline;
            existing.AssignedTo = item.AssignedTo;
            existing.RequestedBy = item.RequestedBy;
            existing.ChangeRequested = item.ChangeRequested;
            existing.VisibleInMinutes = item.VisibleInMinutes;
            existing.RiskLevelid = item.RiskLevelid;
            _context.SaveChanges();

            return MasterDetail(existing.Meetingid, existing, DataDisplayModes.Read);
        }

        [HttpPost]
        public IActionResult CancelEntry(int meetingId)
        {
            return MasterDetail(meetingId, null, DataDisplayModes.Read);
        }

        [HttpPost]
        public IActionResult CancelSelection(int meetingId)
        {
            return MasterDetail(meetingId, null, DataDisplayModes.Read);
        }

        [HttpPost]
        public IActionResult Delete(int meetingId, int meetingItemId)
        {
            var item = _context.MeetingItems.Find(meetingItemId);
            if (item == null)
            {
                return NotFound();
            }
            _context.MeetingItems.Remove(item);
            _context.SaveChanges();

            return MasterDetail(meetingId, null, DataDisplayModes.Read);
        }

        private bool MeetingItemExists(int id)
        {
            return _context.MeetingItems.Any(e => e.MeetingItemID == id);
        }
        
    }
}
