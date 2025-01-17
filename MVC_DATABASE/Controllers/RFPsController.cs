﻿using LinqToExcel;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using MVC_DATABASE.Models;
using MVC_DATABASE.Models.ViewModels;
using Remotion.Data.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace MVC_DATABASE.Controllers
{
    public class RFPsController : Controller
    {
        private EVICEntities db = new EVICEntities();
        public RFPCreate rfpcreate = new RFPCreate();

        // GET: RFPs
        [Authorize(Roles = "Administrator,Employee")]
        public ActionResult Index()
        {
            var open = from x in db.RFPs
                       where x.EXPIRES > DateTime.Now
                       select x;

            rfpcreate.OpenRFPList = new List<RFP>();
            rfpcreate.OpenRFPList = open.ToList<RFP>();

            return View(rfpcreate);
        }

        [Authorize(Roles = "Administrator,Employee")]
        public ActionResult ExpiredIndex()
        {
            var expired = from x in db.RFPs
                          where x.EXPIRES <= DateTime.Now
                          select x;

            rfpcreate.ExpiredRFPList = new List<RFP>();
            rfpcreate.ExpiredRFPList = expired.ToList<RFP>();

            return View(rfpcreate);
        }

        // GET: RFPs/Details/5
        [Authorize(Roles = "Administrator,Employee")]
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            rfpcreate.rfp = await db.RFPs.FindAsync(id);
            if (rfpcreate.rfp == null)
            {
                return HttpNotFound();
            }
            var result = from r in db.VENDORs
                         join p in db.RFPINVITEs
                         on r.Id equals p.Id
                         where p.RFPID == id
                         select r;

            ViewBag.AcceptedVendors = new MultiSelectList(result, "Id", "Organization");
            return View(rfpcreate);
        }

        // GET: RFPs/Create
        [Authorize(Roles = "Administrator,Employee")]
        public ActionResult Create()
        {
            var template = from x in db.TEMPLATEs
                           where x.TYPE == "RFP"
                           select x;
            var expired = from y in db.RFIs
                          where y.EXPIRES <= DateTime.Now
                          select y;

            var vendor = from z in db.VENDORs
                         join c in db.RFIINVITEs
                         on z.Id equals c.Id
                         where c.RFIID == expired.FirstOrDefault().RFIID
                         where !string.IsNullOrEmpty(c.GHX_PATH)
                         select z;

            ViewBag.RFIID = new SelectList(expired, "RFIID", "RFIID");
            ViewBag.TEMPLATEID = new SelectList(template, "TEMPLATEID", "TYPE");
            ViewBag.AcceptedVendors = new MultiSelectList(vendor, "Id", "ORGANIZATION");
            return View();
        }

    

        // POST: RFPs/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Employee")]
        public async Task<ActionResult> Create(RFPCreate model)
        {
            if (ModelState.IsValid)
            {
                model.rfp.RFI = await db.RFIs.FindAsync(model.rfp.RFIID);
                var rFP = new RFP { RFIID = model.rfp.RFIID, CATEGORY = model.rfp.RFI.CATEGORY, TEMPLATEID = model.templateid, CREATED = DateTime.Now, EXPIRES = model.rfp.EXPIRES};
                db.RFPs.Add(rFP);
                if (model.RFPInviteList != null)
                {
                    foreach (var x in model.RFPInviteList)
                    {
                        var rfpinvite = new RFPINVITE { RFPID = rFP.RFPID, Id = x };

                        db.RFPINVITEs.Add(rfpinvite);
                    }
                }

                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
        //if we got this far something failed, reload form
            var template = from x in db.TEMPLATEs
                           where x.TYPE == "RFP"
                           select x;
            var expired = from y in db.RFIs
                          where y.EXPIRES <= DateTime.Now
                          select y;
            ViewBag.RFIID = new SelectList(expired, "RFIID", "RFIID");
            ViewBag.TEMPLATEID = new SelectList(template, "TEMPLATEID", "TYPE");
            ViewBag.AcceptedVendors = new MultiSelectList(db.VENDORs, "Id", "ORGANIZATION");
            return View(model);
        }


        // GET: RFPs/Edit/5
        [Authorize(Roles = "Administrator,Employee")]
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            rfpcreate.rfp = await db.RFPs.FindAsync(id);
            if (rfpcreate.rfp == null)
            {
                return HttpNotFound();
            }
            var vendorRFIQuery = from v in db.VENDORs
                                 join c in db.RFIINVITEs
                                 on v.Id equals c.Id
                                 where c.RFIID == (int)rfpcreate.rfp.RFIID
                                 where !string.IsNullOrEmpty(c.GHX_PATH)
                                 select v;

            var result = from r in db.VENDORs
                         join p in db.RFPINVITEs
                         on r.Id equals p.Id
                         where p.RFPID == id
                         select r;

            vendorRFIQuery = vendorRFIQuery.Where(x => !result.Contains(x));
            ViewBag.AcceptedVendors = new MultiSelectList(result, "Id", "Organization");
            ViewBag.SelectVendors = new MultiSelectList(vendorRFIQuery, "Id", "Organization");

            return View(rfpcreate);
        }

        // POST: RFPs/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator,Employee")]
        public async Task<ActionResult> Edit(RFPCreate model)
        {
            if (ModelState.IsValid)
            {
                var rFP = model.rfp;

                if (model.RFPInviteList != null)
                {
                    foreach (var x in model.RFPInviteList)
                    {
                        var RFPInvite = new RFPINVITE { RFPID = rFP.RFPID, Id = x, OFFER_PATH = string.Empty };
                        db.RFPINVITEs.Add(RFPInvite);
                    }
                }

                await db.SaveChangesAsync();

                return RedirectToAction("Details", "RFPs", new { id = rFP.RFPID});
            }
            //if we got this far something failed, reload form
            var vendorRFIQuery = from v in db.VENDORs
                                 join c in db.RFIINVITEs
                                 on v.Id equals c.Id
                                 where c.RFIID == (int)rfpcreate.rfp.RFIID
                                 select v;

            var result = from r in db.VENDORs
                         join p in db.RFPINVITEs
                         on r.Id equals p.Id
                         where p.RFPID == model.rfp.RFPID
                         select r;
            vendorRFIQuery = vendorRFIQuery.Where(x => !result.Contains(x));
            ViewBag.AcceptedVendors = new MultiSelectList(result, "Id", "Organization");
            ViewBag.SelectVendors = new MultiSelectList(vendorRFIQuery, "Id", "Organization");

            return View(rfpcreate);
        }

        RFPResponse responsemodel = new RFPResponse();

        [Authorize(Roles = "Administrator,Employee")]
        public async Task<ActionResult> VendorResponse(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            responsemodel.RFP = await db.RFPs.FindAsync(id);

            if (responsemodel.RFP == null)
            {
                return HttpNotFound();
            }

            responsemodel.inviteList = new List<RFPINVITE>();
            responsemodel.vendorlist = new List<VENDOR>();
            foreach (var x in db.RFPINVITEs.ToList())
            {

                if (x.RFPID == id)
                {
                    VENDOR vendor = await db.VENDORs.FindAsync(x.Id);
                    responsemodel.inviteList.Add(x);
                    responsemodel.vendorlist.Add(vendor);
                }
            }
            return View(responsemodel);
        }


        [Authorize(Roles = "Administrator,Employee")]
        public FileResult DownloadOffer(string path)
        {

            //select vendors Id from RFPINVITE
            var InviteId = from x in db.RFPINVITEs
                           where x.OFFER_PATH == path
                           select x.Id;
            //Get vendor items from Id
            VENDOR vendor = db.VENDORs.Find(InviteId.FirstOrDefault());
            //select RFPID
            var rfpId = from y in db.RFPINVITEs
                        where y.OFFER_PATH == path
                        select y.RFPID;

            string fileName = (vendor.ORGANIZATION.ToString() + " - " + rfpId.FirstOrDefault().ToString());

            return File(path, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);


        }

        //Vendor RFPs

        [Authorize(Roles = "Vendor")]
        public ActionResult VendorIndex()
        {
            //Establish DB connection.
            EVICEntities dbo = new EVICEntities();
            //Establish List of vendor's RFPs to transfer to View
            VendorRFP VendorRFP = new VendorRFP();
            //Gets user information
            var user_id = User.Identity.GetUserId();
            //List containing VendorRFPs
            VendorRFP.RFPList = new List<RFP>();



            //Query for Vendor's specifi RFPs
            var vendorInvitedRFPs = from i in dbo.RFPINVITEs
                                    join v in dbo.VENDORs on i.Id equals v.Id
                                    join r in dbo.RFPs on i.RFPID equals r.RFPID
                                    where i.Id == user_id
                                    where i.RFP.CREATED <= DateTime.Now
                                    where i.RFP.EXPIRES > DateTime.Now
                                    orderby i.RFPID
                                    select r; //new VendorRFP { rfp = r, rfpInvite = i, vendor = v };

            //Adds queried to list
            VendorRFP.RFPList = vendorInvitedRFPs.ToList();

            return View(VendorRFP);
        }

        RFPVendorRespond.RFPList respondModel = new RFPVendorRespond.RFPList();

        [Authorize(Roles = "Vendor")]
        [HttpGet]
        public async Task<ActionResult> Respond(int Id)
        {

            responsemodel.RFP = await db.RFPs.FindAsync(Id);

            if (responsemodel.RFP.CREATED > DateTime.Now)
            {
                return RedirectToAction("VendorIndex", "RFPs");
            }

            var userID = User.Identity.GetUserId();

            var RFPInvite = from x in db.RFPINVITEs
                            where x.RFPID == Id
                            where x.Id == userID
                            select x;

            responsemodel.RFPInvite = (RFPINVITE)RFPInvite.FirstOrDefault();

            if (!string.IsNullOrEmpty(RFPInvite.FirstOrDefault().OFFER_PATH))
            {
                return RedirectToAction("VendorIndex", "RFPs");
            }

            return View(responsemodel);
        }

        //
        //Stores the uploaded form from View VendorRFI/Respond
        [Authorize(Roles = "Vendor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Respond(RFPResponse model)
        {
            //Verify a file is selected.
            if (model.File != null)
            {
                VENDOR vendor = await db.VENDORs.FindAsync(model.RFPInvite.Id);
                RFPINVITE RFPInvite = await db.RFPINVITEs.FindAsync(model.RFPInvite.PRIMARYKEY);
                string organization = vendor.ORGANIZATION + ".xlsx";
                string RFPID = RFPInvite.RFPID.ToString();
                string offerpath = "~/Content/RFPs/" + RFPID + "/" + organization;


                //Extract the file name.
                var fileName = Path.GetFileName(model.File.FileName);
                //Establishes where to save the path using the extracted name.
                var path = Path.Combine(Server.MapPath("~/Content/RFPs/" + RFPID + "/"), organization);
                RFPInvite.OFFER_PATH = offerpath;

                //checks to see if file path exists, if it doesn't it creates
                var mappath = Server.MapPath("~/Content/RFPs/" + RFPID + "/");
                if (!System.IO.Directory.Exists(mappath))
                    System.IO.Directory.CreateDirectory(mappath);

                //Saves file.
                model.File.SaveAs(path);

                await db.SaveChangesAsync();
                return RedirectToAction("VendorIndex", "RFPs");

            }
            //Sends the user back to their respective RFP Index page.
            return RedirectToAction("VendorIndex", "RFPs");
        }

        [Authorize(Roles = "Vendor")]
        public async Task<ActionResult> ViewDetails(int Id)
        {
            responsemodel.RFP = await db.RFPs.FindAsync(Id);
            if (responsemodel.RFP.CREATED > DateTime.Now)
            {
                return RedirectToAction("VendorIndex", "RFPs");
            }
            var userID = User.Identity.GetUserId();

            var RFPInvite = from x in db.RFPINVITEs
                            where x.RFPID == Id
                            where x.Id == userID
                            select x;

            responsemodel.RFPInvite = (RFPINVITE)RFPInvite.FirstOrDefault();

            return View(responsemodel);
        }

        public FileResult DownloadResponse(string path)
        {

            //select vendors Id from RFIINVITE
            var InviteId = from x in db.RFPINVITEs
                           where x.OFFER_PATH == path
                           select x.Id;
            //Get vendor items from Id
            VENDOR vendor = db.VENDORs.Find(InviteId.FirstOrDefault());
            //select RFIID
            var RFPID = from y in db.RFPINVITEs
                        where y.OFFER_PATH == path
                        select y.RFPID;



            string fileName = (vendor.ORGANIZATION.ToString() + " - " + RFPID.FirstOrDefault().ToString());

            return File(path, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);

        }

        public JsonResult GetAcceptedVendors(string RFIID)
        {
            EVICEntities dbo = new EVICEntities();


            var vendorProductsQuery = from v in dbo.VENDORs
                                      join c in dbo.RFIINVITEs
                                      on v.Id equals c.Id
                                      where c.RFIID.ToString() == RFIID
                                      where !string.IsNullOrEmpty(c.GHX_PATH)
                                      select new { v.Id, v.ORGANIZATION };

            var template = from x in db.TEMPLATEs
                           where x.TYPE == "RFP"
                           select x;
            var expired = from y in db.RFIs
                          where y.EXPIRES <= DateTime.Now
                          select y;

            ViewBag.RFIID = new SelectList(expired, "RFIID", "RFIID");
            ViewBag.TEMPLATEID = new SelectList(template, "TEMPLATEID", "TYPE");

            ViewBag.AcceptedVendors = vendorProductsQuery;

            return Json(vendorProductsQuery, JsonRequestBehavior.AllowGet);
        } 

        //public void RFPAnalytics(string path, RFPVendorRespond.RFPList response)
        //{
        //    string filePath = path;

        //    var excelFile = new ExcelQueryFactory(filePath);

        //    List<ReportAnalytics> Analytics = new List<ReportAnalytics>();

        //    var rfpLines = from l in excelFile.WorksheetRange<ReportAnalytics>("A12", "AA24", "Financial Analysis")
        //                   select l;

        //    foreach (var l in rfpLines)
        //    {

        //        ReportAnalytics AnalyticsLine = new ReportAnalytics();

        //        AnalyticsLine.RFPID = response.RFPInvite.RFPID;
        //        AnalyticsLine.CommodityCode = l.CommodityCode;
        //        AnalyticsLine.Vendor = response.vendor.ORGANIZATION;
        //        AnalyticsLine.ItemVendor = l.ItemVendor;
        //        AnalyticsLine.CommodityName = l.CommodityName;
        //        AnalyticsLine.Category = response.RFP.CATEGORY;
        //        AnalyticsLine.Description = l.Description;
        //        AnalyticsLine.PreviousPriceEach = l.PreviousPriceEach;
        //        AnalyticsLine.NewPriceEach = l.NewPriceEach;

        //        Analytics.Add(AnalyticsLine);
        //    }

        //    //foreach line in analytics, make an object / row in the db.
        //    foreach (ReportAnalytics r in Analytics)
        //    {
        //        if (r.Description != null)
        //        {
        //            //! ! !add to db
        //        }
        //    }
        //}
    }
}