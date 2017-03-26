﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using AstroPhotoGallery.Models;

namespace AstroPhotoGallery.Controllers
{
    public class PictureController : Controller
    {
        // GET: Picture
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        //GET: Picture/List
        public ActionResult List()
        {
            using (var bd = new GalleryDbContext())
            {
                //Get pictures from database

                var pictures = bd.Pictures
                    .Include(x => x.PicUploader)
                    .ToList();

                return View(pictures);
            }
        }

        //GET: Picture/Details
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var db = new GalleryDbContext())
            {
                  //Get picture from batabase

                var picture = db.Pictures.Where(x => x.Id == id).Include(u => u.PicUploader).First();

                if (picture == null)
                {
                    return HttpNotFound();
                }

                return View(picture);
            }
        }

        //GET: Picture/Upload
        public ActionResult Upload()
        {
            return View();
        }

        //POST: Picture/Upload
        [HttpPost]
        public ActionResult Upload(Picture picture)
        {
            if (ModelState.IsValid)
            {
                //Insert pic in database
                using (var db = new GalleryDbContext())
                {
                    //Get uploader id
                    var uploaderId = db.Users
                        .Where(u => u.UserName == this.User.Identity.Name)
                        .First()
                        .Id;

                    //Set picture uploader
                    picture.PicUploaderId = uploaderId;

                    //Save pic in database
                    db.Pictures.Add(picture);
                    db.SaveChanges();

                    RedirectToAction("Index");
                }
            }

            return View(picture);
        }
    }
}