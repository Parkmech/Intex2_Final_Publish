using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Intex2.Models;
using Intex2.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Intex2.Services;

namespace Intex2.Controllers
{
    [Authorize]
    public class BurialCrudController : Controller
    {
        private readonly FagElGamousContext _context;
        private readonly FagElGamousContext _contextFiltered;
        private readonly IS3Service _s3Storage;

        public int pageNum { get; set; } = 1;

        public string sex { get; set; }

        public BurialCrudController(FagElGamousContext context, IS3Service s3)
        {
            _context = context;
            _contextFiltered = _context;
            _s3Storage = s3;
        }


        //Index display page with pagination
        [HttpGet]
        [AllowAnonymous]
        // GET: BurialCrud
        public IActionResult Index(int pageNum = 2)
        {
            int pageSize = 20;

            return View(new BurialListViewModel
            {
                //get burial details 
                Burials = (_context.Burials
                    .OrderBy(b => b.BurialId)
                    .Skip((pageNum - 1) * pageSize)
                    .Take(pageSize)
                    .ToList()
                    ),

                PagingInfo = new PagingInfo
                {
                    ItemsPerPage = pageSize,
                    CurrentPage = pageNum,
                    TotalNumItems = _context.Burials
                    .Count()
                },
                Photos = _context.Photos,

                FieldBooks = _context.FieldBook

            });
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult PagedIndex(BurialListViewModel blvm, int pagenum = 1)
        {
            return View("Index", blvm);
        }


        //Display details for a specific burial based on burialid 
        // GET: BurialCrud/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(string id)
        {
            //burialid gets messed up when passed, convert back to proper format
            string newid = id.Replace("%2F", "/");
            if (newid == null)
            {
                //burialid not found
                //PANIC!   JK we planned for this
                return NotFound();
            }

            //get burial information for one burial based off burialid
            //link with AgeCode, BurialAdultChild, and BurialWrapping
            var burials = await _context.Burials
                .Include(b => b.AgeCodeSingleNavigation)
                .Include(b => b.BurialAdultChildNavigation)
                .Include(b => b.BurialWrappingNavigation)
                .FirstOrDefaultAsync(m => m.BurialId == newid);


            //validate data
            if (burials == null)
            {
                //Secondary precaution if data is bad
                return View("InvalidRecord");
            }

            //return ONE burial to details page 
            return View(burials);
        }

        //Display Invalid record page 
        public IActionResult InvalidRecord()
        {
            return View();
        }


        //Only admins can have access to create 
        [Authorize(Roles = "Admins,Researcher")]
        // GET: BurialCrud/Create
        public IActionResult Create()
        {
            ViewData["AgeCodeSingle"] = new SelectList(_context.AgeCodes, "AgeCode1", "AgeCode1");
            ViewData["BurialAdultChild"] = new SelectList(_context.BurialAdultChildren, "BurialAdultChild1", "BurialAdultChild1");
            ViewData["BurialWrapping"] = new SelectList(_context.BurialWrappings, "BurialWrapping1", "BurialWrapping1");
            return View();
        }

        // POST: BurialCrud/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admins,Researcher")]
        public async Task<IActionResult> Create([Bind("BurialId,BurialId2018,YearOnSkull,MonthOnSkull,DateOnSkull,InitialsOfDataEntryExpert,InitialsOfDataEntryChecker,ByuSample,BodyAnalysis,SkullAtMagazine,PostcraniaAtMagazine,AgeSkull2018,RackAndShelf,ToBeConfirmed,SkullTrauma,PostcraniaTrauma,CribraOrbitala,PoroticHyperostosis,PoroticHyperostosisLocations,MetopicSuture,ButtonOsteoma,PostcraniaTrauma1,OsteologyUnknownComment,TemporalMandibularJointOsteoarthritisTmjOa,LinearHypoplasiaEnamel,AreaHillBurials,Tomb,NsLowPosition,NsHighPosition,NorthOrSouth,EwLowPosition,EwHighPosition,EastOrWest,Square,BurialNumber,BurialWestToHead,BurialWestToFeet,BurialSouthToHead,BurialSouthToFeet,BurialDepth,YearExcav,MonthExcavated,DateExcavated,BurialDirection,BurialPreservation,BurialWrapping,BurialAdultChild,Sex,GenderCode,BurialGenderMethod,AgeCodeSingle,BurialDirection1,NumericMinAge,NumericMaxAge,BurialAgeMethod,HairColorCode,BurialSampleTaken,LengthM,LengthCm,Goods,Cluster,FaceBundle,OsteologyNotes,OtherNotes,SampleNumber,GenderGe,GeFunctionTotal,GenderBodyCol,BasilarSuture,VentralArc,SubpubicAngle,SciaticNotch,PubicBone,PreaurSulcus,MedialIpRamus,DorsalPitting,ForemanMagnum,FemurHead,HumerusHead,Osteophytosis,PubicSymphysis,BoneLength,MedialClavicle,IliacCrest,FemurDiameter,Humerus,FemurLength,HumerusLength,TibiaLength,Robust,SupraorbitalRidges,OrbitEdge,ParietalBossing,Gonian,NuchalCrest,ZygomaticCrest,CranialSuture,MaximumCranialLength,MaximumCranialBreadth,BasionBregmaHeight,BasionNasion,BasionProsthionLength,BizygomaticDiameter,NasionProsthion,MaximumNasalBreadth,InterorbitalBreadth,ArtifactsDescription,PreservationIndex,HairTaken,SoftTissueTaken,BoneTaken,ToothTaken,TextileTaken,DescriptionOfTaken,ArtifactFound,EstimateLivingStature,ToothAttrition,ToothEruption,PathologyAnomalies,EpiphysealUnion,SsmaTimeStamp")] Burial burials)
        {
            burials.BurialId = burials.NorthOrSouth + " " + burials.NsLowPosition + "/" +
                         burials.NsHighPosition + " " + burials.EastOrWest + " " +
                         burials.EwLowPosition + "/" + burials.EwHighPosition + " " +
                         burials.Square + " #" + burials.BurialNumber;


            if (ModelState.IsValid)
            {
                _context.Add(burials);
                await _context.SaveChangesAsync();
                return View("Details", burials);
            }
            ViewData["AgeCodeSingle"] = new SelectList(_context.AgeCodes, "AgeCode1", "AgeCode1", burials.AgeCodeSingle);
            ViewData["BurialAdultChild"] = new SelectList(_context.BurialAdultChildren, "BurialAdultChild1", "BurialAdultChild1", burials.BurialAdultChild);
            ViewData["BurialWrapping"] = new SelectList(_context.BurialWrappings, "BurialWrapping1", "BurialWrapping1", burials.BurialWrapping);
            return View(burials);
        }

        // GET: BurialCrud/Edit/5
        [Authorize(Roles = "Admins,Researcher")]
        public async Task<IActionResult> Edit(string id)
        {
            string newid = id.Replace("%2F", "/");

            if (newid == null)
            {
                return NotFound();
            }

            var burials = await _context.Burials.FindAsync(newid);
            if (burials == null)
            {
                return NotFound();
            }
            ViewData["AgeCodeSingle"] = new SelectList(_context.AgeCodes, "AgeCode1", "AgeCode1", burials.AgeCodeSingle);
            ViewData["BurialAdultChild"] = new SelectList(_context.BurialAdultChildren, "BurialAdultChild1", "BurialAdultChild1", burials.BurialAdultChild);
            ViewData["BurialWrapping"] = new SelectList(_context.BurialWrappings, "BurialWrapping1", "BurialWrapping1", burials.BurialWrapping);
            return View(burials);
        }

        // POST: BurialCrud/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        [Authorize(Roles = "Admins,Researcher")]
        public async Task<IActionResult> Edit([Bind("BurialId,BurialId2018,YearOnSkull,MonthOnSkull,DateOnSkull,InitialsOfDataEntryExpert,InitialsOfDataEntryChecker,ByuSample,BodyAnalysis,SkullAtMagazine,PostcraniaAtMagazine,AgeSkull2018,RackAndShelf,ToBeConfirmed,SkullTrauma,PostcraniaTrauma,CribraOrbitala,PoroticHyperostosis,PoroticHyperostosisLocations,MetopicSuture,ButtonOsteoma,PostcraniaTrauma1,OsteologyUnknownComment,TemporalMandibularJointOsteoarthritisTmjOa,LinearHypoplasiaEnamel,AreaHillBurials,Tomb,NsLowPosition,NsHighPosition,NorthOrSouth,EwLowPosition,EwHighPosition,EastOrWest,Square,BurialNumber,BurialWestToHead,BurialWestToFeet,BurialSouthToHead,BurialSouthToFeet,BurialDepth,YearExcav,MonthExcavated,DateExcavated,BurialDirection,BurialPreservation,BurialWrapping,BurialAdultChild,Sex,GenderCode,BurialGenderMethod,AgeCodeSingle,BurialDirection1,NumericMinAge,NumericMaxAge,BurialAgeMethod,HairColorCode,BurialSampleTaken,LengthM,LengthCm,Goods,Cluster,FaceBundle,OsteologyNotes,OtherNotes,SampleNumber,GenderGe,GeFunctionTotal,GenderBodyCol,BasilarSuture,VentralArc,SubpubicAngle,SciaticNotch,PubicBone,PreaurSulcus,MedialIpRamus,DorsalPitting,ForemanMagnum,FemurHead,HumerusHead,Osteophytosis,PubicSymphysis,BoneLength,MedialClavicle,IliacCrest,FemurDiameter,Humerus,FemurLength,HumerusLength,TibiaLength,Robust,SupraorbitalRidges,OrbitEdge,ParietalBossing,Gonian,NuchalCrest,ZygomaticCrest,CranialSuture,MaximumCranialLength,MaximumCranialBreadth,BasionBregmaHeight,BasionNasion,BasionProsthionLength,BizygomaticDiameter,NasionProsthion,MaximumNasalBreadth,InterorbitalBreadth,ArtifactsDescription,PreservationIndex,HairTaken,SoftTissueTaken,BoneTaken,ToothTaken,TextileTaken,DescriptionOfTaken,ArtifactFound,EstimateLivingStature,ToothAttrition,ToothEruption,PathologyAnomalies,EpiphysealUnion,SsmaTimeStamp,PhotoTaken")] Burial burials)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(burials);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BurialsExists(burials.BurialId))
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
            ViewData["AgeCodeSingle"] = new SelectList(_context.AgeCodes, "AgeCode1", "AgeCode1", burials.AgeCodeSingle);
            ViewData["BurialAdultChild"] = new SelectList(_context.BurialAdultChildren, "BurialAdultChild1", "BurialAdultChild1", burials.BurialAdultChild);
            ViewData["BurialWrapping"] = new SelectList(_context.BurialWrappings, "BurialWrapping1", "BurialWrapping1", burials.BurialWrapping);
            return View(burials);
        }

        // GET: BurialCrud/Delete/5
        [HttpGet]
        [Authorize(Roles = "Admins")]
        public async Task<IActionResult> Delete(string id)
        {
            string newid = id.Replace("%2F", "/");

            if (newid == null)
            {
                return NotFound();
            }

            var burials = await _context.Burials
                .Include(b => b.AgeCodeSingleNavigation)
                .Include(b => b.BurialAdultChildNavigation)
                .Include(b => b.BurialWrappingNavigation)
                .FirstOrDefaultAsync(m => m.BurialId == newid);
            if (burials == null)
            {
                return NotFound();
            }

            return View(burials);
        }


        
        // POST: BurialCrud/Delete/5
        [HttpPost, ActionName("DeleteConf")]
        [Authorize(Roles = "Admins")] //Only admins can delete
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConf(string id)
        {
            //burialid gets messed up when passed, convert back to proper format
            string newid = id.Replace("%2F", "/");

            if (newid == null)
            {
                return NotFound();
            }


            //create lists to populate for tables with a one to many relationship with burials 
            List<Photo> photos = new List<Photo>();
            List<BiologicalSample> bios = new List<BiologicalSample>();
            List<FieldBook> fbook = new List<FieldBook>();
            List<C14> carbSamples = new List<C14>();

            //if ids match, add to list 
            carbSamples.AddRange(_context.C14s.Where(x => x.BurialId == id).ToList());

            if (carbSamples != null)
            {
                //for each item in list remove from database and save changes 
                for (int i = 0; i < carbSamples.Count(); i++)
                {
                    _context.C14s.Remove(carbSamples.FirstOrDefault(p => p.Id == carbSamples[i].Id));
                    await _context.SaveChangesAsync();
                }
            }

            photos.AddRange(_context.Photos.Where(p => p.BurialId == id).ToList());

            if (photos != null)
            {
                for (int i = 0; i < photos.Count(); i++)
                {
                    _context.Photos.Remove(photos.FirstOrDefault(p => p.Id == photos[i].Id));
                    await _context.SaveChangesAsync();
                }
            }
            

            bios.AddRange(_context.BiologicalSamples.Where(p => p.BurialId == id).ToList());

            if (bios != null)
            {
                for (int i = 0; i < bios.Count(); i++)
                {
                    _context.BiologicalSamples.Remove(bios.FirstOrDefault(p => p.Id == bios[i].Id));
                    await _context.SaveChangesAsync();
                }
            }
            

            fbook.AddRange(_context.FieldBook.Where(p => p.BurialId == id).ToList());

            if (fbook != null)
            {
                for (int i = 0; i < photos.Count(); i++)
                {
                    _context.FieldBook.Remove(fbook.FirstOrDefault(p => p.Id == fbook[i].Id));
                    await _context.SaveChangesAsync();
                }
            }
            


            var cranial = await _context.Cranials.FindAsync(newid);

            if (cranial != null)
            {
                _context.Cranials.Remove(cranial);
                await _context.SaveChangesAsync();
            }
            
            //after all of te linking tables are removed, remove burial 
            var burials = await _context.Burials.FindAsync(newid);
            _context.Burials.Remove(burials);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        //Display page is used for sorting and filtering/searching
        [AllowAnonymous]
        public async Task<IActionResult> Display(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            //sort by these attributes
            ViewData["IdSortParm"] = String.IsNullOrEmpty(sortOrder) ? "BurialId_Desc" : "";
            ViewData["SexSortParm"] = sortOrder == "sex_desc" ? "Sex" : "sex_desc";
            ViewData["LengthSortParm"] = sortOrder == "length_desc" ? "Length" : "length_desc";
            ViewData["DepthSortParm"] = sortOrder == "depth_desc" ? "Depth" : "depth_desc";
            ViewData["SquareSortParm"] = sortOrder == "square_desc" ? "Square" : "square_desc";
            ViewData["DirectionSortParm"] = sortOrder == "dir_desc" ? "Dir" : "dir_desc";
            ViewData["EorWSortParm"] = sortOrder == "EorW_desc" ? "EorW" : "EorW_desc";
            ViewData["NorSSortParm"] = sortOrder == "NorS_desc" ? "NorS" : "NorS_desc";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;
            var mummies = from s in _context.Burials select s;
            //var mummies = from s in _context.Burials.IsRequired(false) select s;

            //if user searched for an item, search these fields if they contain it
            if (!String.IsNullOrEmpty(searchString))
            {
                mummies = mummies.Where(s => s.BurialId.Contains(searchString) || s.DescriptionOfTaken.Contains(searchString)
                                       || s.GenderCode.Contains(searchString) || s.EastOrWest.Contains(searchString)
                                       || s.NorthOrSouth.Contains(searchString) || s.BurialDirection.Contains(searchString)
                                       || s.OsteologyNotes.Contains(searchString) || s.OtherNotes.Contains(searchString)
                                       || s.RackAndShelf.Contains(searchString) || s.BurialPreservation.Contains(searchString)
                                       || s.InitialsOfDataEntryChecker.Contains(searchString) || s.InitialsOfDataEntryExpert.Contains(searchString)
                                       || s.GenderGe.Contains(searchString) || s.HairColorCode.Contains(searchString)
                                       || s.HairTaken.Contains(searchString));
            }

            //determine sort order 
            switch (sortOrder)
            {
                case "BurialId_Desc":
                    mummies = mummies.OrderByDescending(s => s.BurialId);
                    break;
                case "Sex":
                    mummies = mummies.OrderBy(s => s.GenderCode);
                    break;
                case "sex_desc":
                    mummies = mummies.OrderByDescending(s => s.GenderCode);
                    break;
                case "Length":
                    mummies = mummies.OrderBy(s => s.LengthM);
                    break;
                case "length_desc":
                    mummies = mummies.OrderByDescending(s => s.LengthM);
                    break;
                case "Depth":
                    mummies = mummies.OrderBy(s => s.BurialDepth);
                    break;
                case "depth_desc":
                    mummies = mummies.OrderByDescending(s => s.BurialDepth);
                    break;
                case "Dir":
                    mummies = mummies.OrderBy(s => s.BurialDirection);
                    break;
                case "dir_desc":
                    mummies = mummies.OrderByDescending(s => s.BurialDirection);
                    break;
                case "Square":
                    mummies = mummies.OrderBy(s => s.Square);
                    break;
                case "square_desc":
                    mummies = mummies.OrderByDescending(s => s.Square);
                    break;
                case "EorW":
                    mummies = mummies.OrderBy(s => s.EastOrWest);
                    break;
                case "EorW_desc":
                    mummies = mummies.OrderByDescending(s => s.EastOrWest);
                    break;
                case "NorS":
                    mummies = mummies.OrderBy(s => s.NorthOrSouth);
                    break;
                case "NorS_desc":
                    mummies = mummies.OrderByDescending(s => s.NorthOrSouth);
                    break;
                default:
                    mummies = mummies.OrderBy(s => s.BurialId);
                    break;
            }

            int pageSize = 20;
            return View(await PaginatedList<Burial>.CreateAsync(mummies.AsNoTracking(), pageNumber ?? 1, pageSize));
            //return View(await mummies.AsNoTracking().ToListAsync());
        }

        //filtering data by attributes
        [AllowAnonymous]
        public IActionResult AdvancedFiltering(BurialListViewModel filterAtr)
        {
            //default values for search 
            string sex = "%";
            string area = "%";
            double length = 0.00;
            double depth = 0.00;
            string bdirection = "%";
            string nors = "%";
            string eorw = "%";
            string burialid = "%";
            string haircolor = "%";

            //set variables if user used filters 
            if (filterAtr.FilterItems != null)
            {
                sex = filterAtr.FilterItems.Sex;
                area = filterAtr.FilterItems.Area;
                length = filterAtr.FilterItems.Length;
                depth = filterAtr.FilterItems.Depth;
                bdirection = filterAtr.FilterItems.BDirection;
                nors = filterAtr.FilterItems.NorS;
                eorw = filterAtr.FilterItems.EorW;
                burialid = filterAtr.FilterItems.BurialId;
                haircolor = filterAtr.FilterItems.HairColorCode;

            }

            //set % for ALL so it returns all values that arent null
            if (sex == "ALL")
            {
                sex = "%";
            }
            if (area == "ALL")
            {
                area = "%";
            }
            if (bdirection == "ALL")
            {
                bdirection = "%";
            }
            if (nors == "ALL")
            {
                nors = "%";
            }
            if (eorw == "ALL")
            {
                eorw = "%";
            }
            if (haircolor == "ALL")
            {
                haircolor = "%";
            }

            burialid = "%" + burialid + "%";

            return View(new BurialListViewModel
            {
                //filter based off users selections
                //users selection is passed through variable to prevent SQL injections 
                Burials = _context.Burials
                    .FromSqlInterpolated($"SELECT * FROM Burials WHERE Gender_Code LIKE {sex} AND Square LIKE {area} AND Burial_Direction LIKE {bdirection} AND North_or_South LIKE {nors} AND East_or_West LIKE {eorw} AND BurialID LIKE {burialid} AND Hair_Color_Code LIKE {haircolor}")
                    .Where(b => b.LengthM >= length)
                    .Where(b => b.BurialDepth >= depth)
                    .OrderBy(b => b.BurialId)

                    .ToList(),
            });
        }

        private bool BurialsExists(string id)
        {
            return _context.Burials.Any(e => e.BurialId == id);
        }

        //need to be a researcher to upload a photo
        [Authorize(Roles = "Admins,Researcher")]
        public IActionResult UploadPhoto(string id)
        {
            string newid = id.Replace("%2F", "/");

            if (newid == null)
            {
                return NotFound();
            }

            BurialListViewModel blvm = new BurialListViewModel {
                burial = _context.Burials.Where(x => x.BurialId == newid).FirstOrDefault()
                          
        };

            return View(blvm);
        }

        [Authorize(Roles="Researcher,Admins")]
        public async Task<IActionResult> SavePhoto(BurialListViewModel photo)
        {
            // magic happens here
            // check if model is not empty
            //Photo uploadPhoto = (Photo)photo.ImageUpload;

            var x = photo.ImageUpload;

            string id = x.BurialId;

            string fileName = x.file.FileName;

            //if model is valid, add to AWS S3 and add data to database 
            if (ModelState.IsValid)
            {
                // create new entity
                await _s3Storage.AddItem(photo.ImageUpload.file, "ForFun");


                Photo PhotoTable = new Photo
                {
                    BurialId = x.BurialId,
                    PhotoId = fileName,
                    Burial = _context.Burials.Where(x => x.BurialId == id).FirstOrDefault()
                };

                Burial bur = _context.Burials.Where(x => x.BurialId == id).FirstOrDefault();

                _context.Photos.Add(PhotoTable);
                await _context.SaveChangesAsync();

                return View("Details", bur);
            }
            else
            {
                return View("Home");
            }

        }
    }
}
