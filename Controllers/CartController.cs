using Microsoft.AspNetCore.Mvc;
using QuanLySanPhamApp.Data;
using QuanLySanPhamApp.Models;
using QuanLySanPhamApp.Services;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace QuanLySanPhamApp.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ShoppingCartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext context, ShoppingCartService cartService, ILogger<CartController> logger)
        {
            _context = context;
            _cartService = cartService;
            _logger = logger;
        }

        // GET: Cart
        public IActionResult Index()
        {
            try
            {
                var cartItems = _cartService.GetCartItems();
                ViewBag.CartTotal = _cartService.GetTotal();
                ViewBag.CartCount = _cartService.GetCount();
                return View(cartItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart: {Message}", ex.Message);
                TempData["ErrorMessage"] = "The shopping cart is currently unavailable. Please try again later.";
                return View(new List<CartItem>());
            }
        }

        // POST: Cart/AddToCart/5
        [HttpPost]
        public async Task<IActionResult> AddToCart(int id, int quantity = 1)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product != null)
                {
                    _cartService.AddToCart(product, quantity);
                    TempData["SuccessMessage"] = $"{product.Name} added to your cart";
                }
                else
                {
                    TempData["ErrorMessage"] = "Product not found";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Could not add item to cart.";
            }
            
            // Return to the previous page if available, or to product listing
            if (Request.Headers["Referer"].ToString() != "")
                return Redirect(Request.Headers["Referer"].ToString());
            else
                return RedirectToAction("Index", "Product");
        }
        
        // GET: Cart/RemoveFromCart/5
        public IActionResult RemoveFromCart(int id)
        {
            try
            {
                _cartService.RemoveFromCart(id);
                TempData["SuccessMessage"] = "Item removed from cart";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Could not remove item from cart";
            }
            return RedirectToAction("Index");
        }
        
        // POST: Cart/UpdateQuantity
        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            try
            {
                _cartService.UpdateQuantity(id, quantity);
                TempData["SuccessMessage"] = "Cart updated";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Could not update cart quantity";
            }
            return RedirectToAction("Index");
        }
        
        // GET: Cart/Clear
        public IActionResult Clear()
        {
            try
            {
                _cartService.ClearCart();
                TempData["SuccessMessage"] = "Cart cleared";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Could not clear cart";
            }
            return RedirectToAction("Index");
        }
    }
}
