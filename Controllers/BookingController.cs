using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using VehicleBooking.Models;

namespace VehicleBooking.Controllers
{
    public class BookingController : Controller
    {
        private readonly string _connStr;

        public BookingController(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("DefaultConnection");
        }

        // ============================
        //      DASHBOARD PAGE
        // ============================
        public IActionResult Dashboard()
        {
            return View();
        }

        // ============================
        //   VIEW ALL BOOKINGS PAGE
        // ============================
        public IActionResult ViewBookings()
        {
            List<Booking> bookings = new List<Booking>();

            using (var conn = new MySqlConnection(_connStr))
            {
                conn.Open();
                string query = "SELECT * FROM VehicleBookings ORDER BY BookingDate DESC";

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bookings.Add(new Booking
                        {
                            BookingID = reader.GetInt32("BookingID"),
                            FullName = reader.GetString("FullName"),
                            VehicleType = reader.GetString("VehicleType"),
                            PickupDate = reader.GetDateTime("PickupDate"),
                            ReturnDate = reader.GetDateTime("ReturnDate"),
                            ContactNumber = reader.GetString("ContactNumber"),
                            BookingDate = reader.GetDateTime("BookingDate")
                        });
                    }
                }
            }

            return View(bookings);
        }

        // ============================
        //        CREATE BOOKING
        // ============================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Booking());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Booking model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.PickupDate == null || model.ReturnDate == null)
            {
                ModelState.AddModelError("", "Pickup date and Return date are required.");
                return View(model);
            }

            if (model.PickupDate.Value.Date > model.ReturnDate.Value.Date)
            {
                ModelState.AddModelError("", "Pickup date must be before or same as Return date.");
                return View(model);
            }

            if (model.PickupDate.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError("", "Pickup date cannot be in the past.");
                return View(model);
            }

            try
            {
                int insertedId = 0;

                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();
                    string sql = @"
                        INSERT INTO VehicleBookings 
                        (FullName, VehicleType, PickupDate, ReturnDate, ContactNumber)
                        VALUES 
                        (@FullName, @VehicleType, @PickupDate, @ReturnDate, @ContactNumber);
                        SELECT LAST_INSERT_ID();
                    ";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@FullName", model.FullName);
                        cmd.Parameters.AddWithValue("@VehicleType", model.VehicleType);
                        cmd.Parameters.AddWithValue("@PickupDate", model.PickupDate.Value.Date);
                        cmd.Parameters.AddWithValue("@ReturnDate", model.ReturnDate.Value.Date);
                        cmd.Parameters.AddWithValue("@ContactNumber", model.ContactNumber);

                        // Get the inserted booking ID
                        insertedId = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }

                // Redirect to Success page with bookingId
                return RedirectToAction(nameof(Success), new { bookingId = insertedId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving booking: " + ex.Message);
                return View(model);
            }
        }

        // ============================
        //       SUCCESS PAGE
        // ============================
        public IActionResult Success(int bookingId)
        {
            Booking booking = null;

            using (var conn = new MySqlConnection(_connStr))
            {
                conn.Open();
                string query = "SELECT * FROM VehicleBookings WHERE BookingID = @BookingID LIMIT 1";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BookingID", bookingId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            booking = new Booking
                            {
                                BookingID = reader.GetInt32("BookingID"),
                                FullName = reader.GetString("FullName"),
                                VehicleType = reader.GetString("VehicleType"),
                                PickupDate = reader.GetDateTime("PickupDate"),
                                ReturnDate = reader.GetDateTime("ReturnDate"),
                                ContactNumber = reader.GetString("ContactNumber"),
                                BookingDate = reader.GetDateTime("BookingDate")
                            };

                            // --- Assign PricePerDay based on vehicle type ---
                            booking.PricePerDay = booking.VehicleType switch
                            {
                                "Hatchback" => 400m,
                                "Sedan" => 500m,
                                "SUV" => 800m,
                                "Van" => 1200m,
                                _ => 500m
                            };

                            // --- Calculate TotalCost ---
                            booking.TotalCost = ((booking.ReturnDate.Value - booking.PickupDate.Value).Days + 1) * booking.PricePerDay;
                        }
                    }
                }
            }

            if (booking == null)
                return NotFound();

            ViewBag.Message = $"Thank you, {booking.FullName}! Your booking for {booking.VehicleType} is confirmed.";
            return View(booking);
        }

    }
}
