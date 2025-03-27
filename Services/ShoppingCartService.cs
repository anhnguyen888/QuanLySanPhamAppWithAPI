using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using QuanLySanPhamApp.Data;
using QuanLySanPhamApp.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace QuanLySanPhamApp.Services
{
    public class ShoppingCartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _cartSessionKey = "ShoppingCart";
        private readonly ILogger<ShoppingCartService> _logger;
        
        public ShoppingCartService(IHttpContextAccessor httpContextAccessor, ILogger<ShoppingCartService> logger = null)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }
        
        // Helper methods to get and save cart from/to session
        private List<CartItem> GetCartItemsFromSession()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            string cartJson = session.GetString(_cartSessionKey);
            
            if (string.IsNullOrEmpty(cartJson))
                return new List<CartItem>();
            
            try
            {
                return JsonSerializer.Deserialize<List<CartItem>>(cartJson);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deserializing cart from session");
                return new List<CartItem>();
            }
        }
        
        private void SaveCartItemsToSession(List<CartItem> cartItems)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            
            try
            {
                string cartJson = JsonSerializer.Serialize(cartItems);
                session.SetString(_cartSessionKey, cartJson);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error serializing cart to session");
            }
        }
        
        public void AddToCart(Product product, int quantity = 1)
        {
            if (product == null)
                return;
                
            try
            {
                var cartItems = GetCartItemsFromSession();
                
                var existingItem = cartItems.FirstOrDefault(c => c.ProductId == product.ProductId);
                
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    // Generate a new ID (use timestamp/ticks for uniqueness)
                    int newId = (int)(DateTime.Now.Ticks % int.MaxValue);
                    
                    cartItems.Add(new CartItem
                    {
                        Id = newId,
                        ProductId = product.ProductId,
                        ProductName = product.Name,
                        Price = product.Price,
                        Quantity = quantity,
                        DateCreated = DateTime.Now
                    });
                }
                
                SaveCartItemsToSession(cartItems);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding item to cart");
            }
        }
        
        public void RemoveFromCart(int id)
        {
            try
            {
                var cartItems = GetCartItemsFromSession();
                var itemToRemove = cartItems.FirstOrDefault(c => c.Id == id);
                
                if (itemToRemove != null)
                {
                    cartItems.Remove(itemToRemove);
                    SaveCartItemsToSession(cartItems);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing item from cart");
            }
        }
        
        public void UpdateQuantity(int id, int quantity)
        {
            try
            {
                if (quantity <= 0)
                    return;
                    
                var cartItems = GetCartItemsFromSession();
                var item = cartItems.FirstOrDefault(c => c.Id == id);
                
                if (item != null)
                {
                    item.Quantity = quantity;
                    SaveCartItemsToSession(cartItems);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating cart item quantity");
            }
        }
        
        public List<CartItem> GetCartItems()
        {
            return GetCartItemsFromSession();
        }
        
        public decimal GetTotal()
        {
            try
            {
                var cartItems = GetCartItemsFromSession();
                return cartItems.Sum(item => item.Price * item.Quantity);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calculating cart total");
                return 0;
            }
        }
        
        public int GetCount()
        {
            try
            {
                var cartItems = GetCartItemsFromSession();
                return cartItems.Sum(item => item.Quantity);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting cart count");
                return 0;
            }
        }
        
        public void ClearCart()
        {
            try
            {
                _httpContextAccessor.HttpContext.Session.Remove(_cartSessionKey);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error clearing cart");
            }
        }
    }
}
