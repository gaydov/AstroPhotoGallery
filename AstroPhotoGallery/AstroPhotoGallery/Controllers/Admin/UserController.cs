﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AstroPhotoGallery.Extensions;
using AstroPhotoGallery.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using PagedList;

namespace AstroPhotoGallery.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        // GET: User
        public ActionResult Index(string sortOrder, string searchUser, string currentFilter, int? page)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.Email = String.IsNullOrEmpty(sortOrder) ? "Email_desc" : "";

            if (searchUser != null)
            {
                page = 1;
            }
            else
            {
                searchUser = currentFilter;
            }

            ViewBag.CurrentFilter = searchUser;

            using (var db = new GalleryDbContext())
            {
                var users = db.Users
                    .ToList();

                if (!String.IsNullOrEmpty(searchUser))
                {
                    users = users.Where(s => s.Email.Contains(searchUser)).ToList();
                }

                var admins = GetAdminUserNames(users, db);

                ViewBag.Admins = admins;

                switch (sortOrder)
                {
                    case "Email_desc":
                        users = users.OrderByDescending(s => s.Email).ToList();
                        break;
                }

                int pageSize = 3;
                int pageNumber = (page ?? 1);
                return View(users.ToPagedList(pageNumber, pageSize));
            }
        }

        //GET: User/Edit
        public ActionResult Edit(string id)
        {
            //Validate id
            if (id == null)
            {
                this.AddNotification("No user ID provided.", NotificationType.ERROR);
                return RedirectToAction("Index");
            }

            using (var db = new GalleryDbContext())
            {
                //Get user from database
                var user = db.Users.Where(u => u.Id == id).FirstOrDefault();

                //Check if user exists
                if (user == null)
                {
                    this.AddNotification("User doesn't exist.", NotificationType.ERROR);
                    return RedirectToAction("Index");
                }

                //Create a view model
                var model = new EditUserViewModel();
                model.User = user;
                model.Roles = GetUserRoles(user, db);

                //Pass the model to the view
                return View(model);
            }
        }

        //POST: User/Edit
        [HttpPost]
        public ActionResult Edit(string id, EditUserViewModel viewModel)
        {
            //Check if model is valid
            if (ModelState.IsValid)
            {
                using (var db = new GalleryDbContext())
                {
                    //Get user from database 
                    var user = db
                        .Users
                        .FirstOrDefault(u => u.Id == id);

                    //Check if user exists
                    if (user == null)
                    {
                        this.AddNotification("User doesn't exist.", NotificationType.ERROR);
                        return RedirectToAction("Index");
                    }

                    //If password field is not empty, change password
                    if (!string.IsNullOrEmpty(viewModel.Password))
                    {
                        var hasher = new PasswordHasher();
                        var passwordHash = hasher.HashPassword(viewModel.Password);
                        user.PasswordHash = passwordHash;
                    }

                    //Set user properties
                    user.Email = viewModel.User.Email;
                    user.FirstName = viewModel.User.FirstName;
                    user.LastName = viewModel.User.LastName;
                    user.UserName = viewModel.User.Email;
                    this.SetUserRoles(viewModel, user, db);

                    //Save changes
                    db.Entry(user).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
            }

            return View(viewModel);
        }

        //GET: User/Delete
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                this.AddNotification("No user ID provided.", NotificationType.ERROR);
                return RedirectToAction("Index");
            }

            using (var db = new GalleryDbContext())
            {
                //Get user from database
                var user = db.Users
                    .Where(u => u.Id == id)
                    .FirstOrDefault();

                //Check if user exists
                if (user == null)
                {
                    this.AddNotification("User doesn't exist.", NotificationType.ERROR);
                    return RedirectToAction("Index");
                }

                return View(user);
            }
        }

        //POST: User/Delete
        [HttpPost]
        [ActionName("Delete")]
        public ActionResult DeleteConfirmed(string id)
        {
            if (id == null)
            {
                this.AddNotification("No user ID provided.", NotificationType.ERROR);
                return RedirectToAction("Index");
            }

            using (var db = new GalleryDbContext())
            {
                //Get user from database
                var user = db.Users
                    .Where(u => u.Id == id)
                    .FirstOrDefault();

                //Get user pictures form database
                var userPictures = db.Pictures
                    .Where(p => p.PicUploader.Id == user.Id);

                //Delete user pictures
                foreach (var picture in userPictures)
                {
                    db.Pictures.Remove(picture);
                }

                //Delete user and save changes 
                db.Users.Remove(user);
                db.SaveChanges();

                return RedirectToAction("Index");
            }
        }

        private void SetUserRoles(EditUserViewModel model, ApplicationUser user, GalleryDbContext db)
        {
            var userManager = Request
                .GetOwinContext()
                .GetUserManager<ApplicationUserManager>();

            foreach (var role in model.Roles)
            {
                if (role.IsSelected)
                {
                    userManager.AddToRole(user.Id, role.Name);
                }
                else
                {
                    userManager.RemoveFromRole(user.Id, role.Name);
                }
            }
        }

        private IList<Role> GetUserRoles(ApplicationUser user, GalleryDbContext db)
        {
            //Create user manager 
            var userManager = Request
                .GetOwinContext()
                .GetUserManager<ApplicationUserManager>();

            //Get all application roles
            var roles = db.Roles
                .Select(r => r.Name)
                .OrderBy(r => r)
                .ToList();

            //For each application role, check if the user has it
            var userRoles = new List<Role>();

            foreach (var roleName in roles)
            {
                var role = new Role { Name = roleName };

                if (userManager.IsInRole(user.Id, roleName))
                {
                    role.IsSelected = true;
                }

                userRoles.Add(role);
            }
            //Return a list with all roles

            return userRoles;
        }

        private HashSet<string> GetAdminUserNames(List<ApplicationUser> users, GalleryDbContext context)
        {
            var userManager = new UserManager<ApplicationUser>(
                new UserStore<ApplicationUser>(context));

            var admins = new HashSet<string>();

            foreach (var user in users)
            {
                if (userManager.IsInRole(user.Id, "Admin"))
                {
                    admins.Add(user.UserName);
                }
            }

            return admins;
        }
    }
}