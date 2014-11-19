﻿using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System;
using System.IO;
using System.Globalization;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using MVC_DATABASE.Models;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Web.Security;
using Microsoft.AspNet.Identity.EntityFramework;
using MVC_DATABASE.Models.ViewModels;

namespace MVC_DATABASE.Controllers
{
    public class CONTRACTsController : Controller
    {
        private EVICEntities db = new EVICEntities();
        private ContractIndex contractindex = new ContractIndex();

        // GET: CONTRACTs
        public ActionResult Index()
        {
            var indexview = from x in db.CONTRACTs
                            join y in db.RFPs
                            on x.RFPID equals y.RFPID
                            join z in db.VENDORs
                            on x.Id equals z.Id
                            select new ContractIndex { contractID = x.CONTRACTID, rfpID = y.RFPID, category = y.CATEGORY, contractPath = x.CONTRACT_PATH, organization = z.ORGANIZATION, CREATED = y.CREATED, EXPIRES = y.EXPIRES};
                        
            return View(indexview.ToList<ContractIndex>());
        }

        // GET: CONTRACTs/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CONTRACT cONTRACT = await db.CONTRACTs.FindAsync(id);
            if (cONTRACT == null)
            {
                return HttpNotFound();
            }
            RFP rfp = await db.RFPs.FindAsync(cONTRACT.RFPID);
            ViewBag.Category = rfp.CATEGORY;

            return View(cONTRACT);
        }

        public JsonResult GetAcceptedVendors(int RFPID)
        {
            EVICEntities dbo = new EVICEntities();

            var vendorProductsQuery = from v in dbo.VENDORs
                                      join c in dbo.RFPINVITEs
                                      on v.Id equals c.Id
                                      join r in dbo.RFPs
                                      on c.RFPID equals r.RFPID
                                      where c.OFFER_PATH != string.Empty
                                      where r.RFPID == RFPID
                                      select new {v.Id, v.ORGANIZATION };

            ViewBag.AcceptedVendors = vendorProductsQuery;

            return Json(vendorProductsQuery, JsonRequestBehavior.AllowGet);
        } 

        // GET: CONTRACTs/Create
        public ActionResult Create()
        {
            var rfpidquery = from x in db.RFPs
                             where x.EXPIRES <= DateTime.Now
                             select x.RFPID;

            ViewBag.rfpid = rfpidquery;

            var templates = from x in db.TEMPLATEs
                            where x.TYPE == "CONTRACT"
                            select x;

            ViewBag.templates = new SelectList(templates, "TEMPLATEID", "TEMPLATEID");

            var vendorProductsQuery = from v in db.VENDORs
                                      join c in db.RFPINVITEs
                                      on v.Id equals c.Id
                                      join r in db.RFPs
                                      on c.RFPID equals r.RFPID
                                      where c.OFFER_PATH != string.Empty
                                      where r.RFPID == rfpidquery.FirstOrDefault()
                                      select new { v.Id, v.ORGANIZATION };

            ViewBag.AcceptedVendors = vendorProductsQuery;

            return View();
        }

        // POST: CONTRACTs/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateContract model)
        {
            if (ModelState.IsValid)
            {
                CONTRACT con = new CONTRACT { Id = model.contract.Id, TEMPLATEID = model.contract.TEMPLATEID, RFPID = model.contract.RFPID, CREATED = DateTime.Now, EXPIRES = model.contract.EXPIRES };
                
                //if (model.file != null)
                //{
                    //Extract the file name.
                    var fileName = Path.GetFileName(model.file.FileName);
                    //Establishes where to save the path using the extracted name.
                    var path = Path.Combine(Server.MapPath(@"~/Content/File_Uploads/"), fileName);
                    //Saves file.
                    model.file.SaveAs(path);
                    con.DOCUMENTPATH = path;
                //}

                db.CONTRACTs.Add(con);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            var rfpidquery = from x in db.RFPs
                             where x.EXPIRES <= DateTime.Now
                             select x.RFPID;

            ViewBag.rfpid = rfpidquery;

            var templates = from x in db.TEMPLATEs
                            where x.TYPE == "CONTRACT"
                            select x;

            ViewBag.templates = new SelectList(templates, "TEMPLATEID", "TEMPLATEID");

            var vendorProductsQuery = from v in db.VENDORs
                                      join c in db.RFPINVITEs
                                      on v.Id equals c.Id
                                      join r in db.RFPs
                                      on c.RFPID equals r.RFPID
                                      where c.OFFER_PATH != string.Empty
                                      where r.RFPID == rfpidquery.FirstOrDefault()
                                      select new { v.Id, v.ORGANIZATION };

            ViewBag.AcceptedVendors = vendorProductsQuery;
            return View(model);
        }

        [Authorize(Roles = "Administrator,Employee,Vendor")]
        public FileResult DownloadTemplate(string path)
        {

            //select contract id
            var templateId = from y in db.TEMPLATEs
                             where y.PATH == path
                             select y.TEMPLATEID;

            string fileName = ("Contract Template - " + templateId.FirstOrDefault().ToString());

            return File(path, "application/pdf", fileName);
        }

        [Authorize(Roles = "Administrator,Employee,Vendor")]
        public FileResult DownloadContract(string path)
        {

            //select vendors Id from contract
            var InviteId = from x in db.CONTRACTs
                           where x.CONTRACT_PATH == path
                           select x.Id;
            //Get vendor items from Id
            VENDOR vendor = db.VENDORs.Find(InviteId.FirstOrDefault());
            //select contract id
            var contractId = from y in db.CONTRACTs
                        where y.CONTRACT_PATH == path
                        where y.Id == vendor.Id
                        select y.CONTRACTID;

            string fileName = (vendor.ORGANIZATION.ToString() + " - " + contractId.FirstOrDefault());

            return File(path, "application/pdf", fileName);
        }

        // GET: CONTRACTs/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CONTRACT cONTRACT = await db.CONTRACTs.FindAsync(id);
            if (cONTRACT == null)
            {
                return HttpNotFound();
            }
            ViewBag.Id = new SelectList(db.AspNetUsers, "Id", "Email", cONTRACT.Id);
            ViewBag.TEMPLATEID = new SelectList(db.TEMPLATEs, "TEMPLATEID", "TYPE", cONTRACT.TEMPLATEID);
            return View(cONTRACT);
        }

        // POST: CONTRACTs/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "CONTRACTID,Id,TEMPLATEID,RFPID,CONTRACT_PATH")] CONTRACT cONTRACT)
        {
            if (ModelState.IsValid)
            {
                db.Entry(cONTRACT).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.Id = new SelectList(db.AspNetUsers, "Id", "Email", cONTRACT.Id);
            ViewBag.TEMPLATEID = new SelectList(db.TEMPLATEs, "TEMPLATEID", "TYPE", cONTRACT.TEMPLATEID);
            return View(cONTRACT);
        }

        // GET: CONTRACTs/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CONTRACT cONTRACT = await db.CONTRACTs.FindAsync(id);
            if (cONTRACT == null)
            {
                return HttpNotFound();
            }
            return View(cONTRACT);
        }

        // POST: CONTRACTs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            CONTRACT cONTRACT = await db.CONTRACTs.FindAsync(id);
            db.CONTRACTs.Remove(cONTRACT);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        //Beginning of vendor
        VendorContract responsemodel = new VendorContract();

        public async Task<ActionResult> VendorResponse(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            responsemodel.contract = await db.CONTRACTs.FindAsync(id);

            if (responsemodel.contract == null)
            {
                return HttpNotFound();
            }

            responsemodel.contractlist = new List<CONTRACT>();
            responsemodel.vendorlist = new List<VENDOR>();

            foreach (var x in db.CONTRACTs.ToList())
            {

                if (x.CONTRACTID == id)
                {
                    VENDOR vendor = await db.VENDORs.FindAsync(x.Id);
                    responsemodel.contractlist.Add(x);
                    responsemodel.vendorlist.Add(vendor);
                }
            }

            return View(responsemodel);
        }

        public ActionResult Respond(string Id)
        {
           
            var response = new CONTRACT { Id = Id };

            return View();
        }

        //
        //Stores the uploaded form from View VendorRFI/Respond
        
        [HttpPost]
        public ActionResult Respond(HttpPostedFileBase file)
        {
            //Verify a file is selected.
            if (file != null)
            {
                //Extract the file name.
                var fileName = Path.GetFileName(file.FileName);
                //Establishes where to save the path using the extracted name.
                var path = Path.Combine(Server.MapPath(@"~/Content/ContractStore/TestContracts"), fileName);
                //Saves file.
                file.SaveAs(path);
            }
            //Sends the user back to their respective RFI Index page.
            return RedirectToAction("VendorIndex");
        }

        //Vendor Document uploads
        public ActionResult VendorDocUpload(string Id)
        {

            var response = new CONTRACT { Id = Id };

            return View();
        }


        [HttpPost]
        public ActionResult VendorDocUpload(HttpPostedFileBase file)
        {
            //Verify a file is selected.
            if (file != null)
            {
                //Extract the file name.
                var fileName = Path.GetFileName(file.FileName);
                //Establishes where to save the path using the extracted name.
                var path = Path.Combine(Server.MapPath(@"~/Content/File_Uploads/VendorDocUploads"), fileName);
                //Saves file.
                file.SaveAs(path);
            }
            
            return RedirectToAction("Details");
        }

        public ActionResult VendorDocDownload(string fileName)
        {
            var dir = new System.IO.DirectoryInfo(Server.MapPath("~/Content/File_Uploads/VendorDocUploads"));
            System.IO.FileInfo[] fileNames = dir.GetFiles("*.*");
            List<string> items = new List<string>();

            foreach (var file in fileNames)
            {
                items.Add(file.Name);
            }

            return View(items);
        }

        public async Task<ActionResult> VendorDetails(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CONTRACT cONTRACT = await db.CONTRACTs.FindAsync(id);
            if (cONTRACT == null)
            {
                return HttpNotFound();
            }
            RFP rfp = await db.RFPs.FindAsync(cONTRACT.RFPID);
            ViewBag.Category = rfp.CATEGORY;

            return View(cONTRACT);
        }


        VendorContractIndex vendorIndex = new VendorContractIndex();

        public ActionResult VendorIndex()
        {
            EVICEntities db = new EVICEntities();
            var user = User.Identity.GetUserId();

            var indexview = from x in db.CONTRACTs
                            join y in db.RFPs
                            on x.RFPID equals y.RFPID
                            join z in db.VENDORs
                            on x.Id equals z.Id
                            where z.Id == user
                            select new VendorContractIndex { contractID = x.CONTRACTID, rfpID = y.RFPID, category = y.CATEGORY, contractPath = x.CONTRACT_PATH};

            return View(indexview.ToList<VendorContractIndex>());
        } 	
    }
}
