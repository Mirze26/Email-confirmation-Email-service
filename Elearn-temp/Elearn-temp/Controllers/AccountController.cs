﻿using Elearn_temp.Models;
using Elearn_temp.ViewModels.Account;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MimeKit.Text;
using MimeKit;
using MailKit.Net.Smtp;
using Elearn_temp.Services.Interfaces;

namespace Elearn_temp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager; 

        private readonly SignInManager<AppUser> _signInManager; 

        private readonly IEmailService _emailService;

        public AccountController(UserManager<AppUser> userManager,
                               SignInManager<AppUser> signInManager,
                                IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
        }


        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            AppUser newUser = new()
            {
                UserName = model.Username,
                Email = model.Email,
                FullName = model.FullName,
            };

            IdentityResult result = await _userManager.CreateAsync(newUser, model.Password); 


            if (!result.Succeeded)
            {
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, item.Description);
                }

                return View(model);
            }

        

            string token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);

            string link = Url.Action(nameof(ConfirmEmail),"Account", new {userId = newUser.Id,token}, Request.Scheme, Request.Host.ToString());

            string subject = "Register confirmation";

            string html = string.Empty;

            using (StreamReader reader = new StreamReader("wwwroot/templates/verify.html"))
            {
                html = reader.ReadToEnd(); 
            }


            html = html.Replace("{{link}}", link);
            html = html.Replace("{{HeaderText}}", "Hello P135");
                

         

            _emailService.Send(newUser.Email, subject, html);



            


            // create email message
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse("mirzaab@code.edu.az"));
            email.To.Add(MailboxAddress.Parse(newUser.Email));
            email.Subject = "Register confirmation";
            email.Body = new TextPart(TextFormat.Html);

            // send email
            using var smtp = new SmtpClient();
            smtp.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            smtp.Authenticate("mirzaab@code.edu.az", "gxlaldkdolgfzyez");
            smtp.Send(email);
            smtp.Disconnect(true);




            return RedirectToAction(nameof(VerifyEmail));

        }

        public async Task <IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null) return BadRequest();

            AppUser user = await _userManager.FindByIdAsync(userId);

            if (user == null) return NotFound();

            await _userManager.ConfirmEmailAsync(user, token);

            await _signInManager.SignInAsync(user, false);

            return RedirectToAction("Index","Home");
        }

        public IActionResult VerifyEmail()
        {
            return View();
        }



        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            AppUser user = await _userManager.FindByEmailAsync(model.EmailOrUsername);  

            if (user is null)  
            {
                user = await _userManager.FindByNameAsync(model.EmailOrUsername);  

            }

            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Email or password is wrong");  
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Email or password is wrong");  
                return View(model);

            }

            return RedirectToAction("Index", "Home");
        }




        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();  
            return RedirectToAction("Index", "Home");
        }




    }
}
