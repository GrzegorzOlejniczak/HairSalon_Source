// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using HairSalonApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace HairSalonApp.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        public string Firstname { get; set; }
        public string Lastname { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Phone]
            [Display(Name = "Numer telefonu")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Zdjęcie profilowe")]
            public IFormFile ProfilePicture { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;


            Input = new InputModel
            {
                PhoneNumber = phoneNumber,

            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            //var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            //if (Input.PhoneNumber != phoneNumber)
            //{
            //    var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
            //    if (!setPhoneResult.Succeeded)
            //    {
            //        StatusMessage = "Unexpected error when trying to set phone number.";
            //        return RedirectToPage();
            //    }
            //}

            if (Input.ProfilePicture != null && Input.ProfilePicture.Length > 0)
            {
                Debug.WriteLine("Profile picture uploaded by the user is not null and has a length greater than 0.");

                // Sprawdź, czy rozszerzenie pliku jest dozwolone
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(Input.ProfilePicture.FileName).ToLower();
                Debug.WriteLine($"File extension detected: {fileExtension}");

                if (!allowedExtensions.Contains(fileExtension))
                {
                    Debug.WriteLine("Invalid file extension. Adding error to ModelState.");
                    ModelState.AddModelError("ProfilePicture", "Dozwolone są tylko pliki graficzne.");
                    await LoadAsync(user);
                    return Page();
                }

                // Generowanie unikalnej nazwy pliku
                var fileName = Guid.NewGuid().ToString() + fileExtension;
                Debug.WriteLine($"Generated unique file name: {fileName}");

                // Ścieżka do katalogu, gdzie chcesz zapisać plik
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles");
                Debug.WriteLine($"Upload path resolved: {uploadPath}");

                // Utwórz katalog, jeśli nie istnieje
                if (!Directory.Exists(uploadPath))
                {
                    Debug.WriteLine("Directory does not exist. Creating directory.");
                    Directory.CreateDirectory(uploadPath);
                }

                // Ścieżka do pliku
                var filePath = Path.Combine(uploadPath, fileName);
                Debug.WriteLine($"Full file path resolved: {filePath}");

                try
                {
                    // Zapisz plik na dysku
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await Input.ProfilePicture.CopyToAsync(stream);
                    }
                    Debug.WriteLine("File successfully saved on the disk.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error while saving the file: {ex.Message}");
                    ModelState.AddModelError("ProfilePicture", "Wystąpił błąd podczas zapisywania pliku. Spróbuj ponownie.");
                    await LoadAsync(user);
                    return Page();
                }

                // Zaktualizuj ścieżkę do pliku w bazie danych
                user.ProfilePicturePath = "/images/profiles/" + fileName;
                var updateResult = await _userManager.UpdateAsync(user);

                if (updateResult.Succeeded)
                {
                    Debug.WriteLine("User profile updated successfully in the database.");
                }
                else
                {
                    Debug.WriteLine("Failed to update user profile in the database.");
                    foreach (var error in updateResult.Errors)
                    {
                        Debug.WriteLine($"Error: {error.Description}");
                    }
                    ModelState.AddModelError("ProfilePicture", "Wystąpił błąd podczas aktualizacji profilu użytkownika.");
                    await LoadAsync(user);
                    return Page();
                }
            }
            else
            {
                Debug.WriteLine("Input.ProfilePicture is either null or has a length of 0.");
            }


            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
