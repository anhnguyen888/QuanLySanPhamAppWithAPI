using Microsoft.AspNetCore.Mvc;
using QuanLySanPhamApp.Services;
using System.Threading.Tasks;

namespace QuanLySanPhamApp.ViewComponents
{
    public class CartSummaryViewComponent : ViewComponent
    {
        private readonly ShoppingCartService _cartService;

        public CartSummaryViewComponent(ShoppingCartService cartService)
        {
            _cartService = cartService;
        }

        public IViewComponentResult Invoke()
        {
            var count = _cartService.GetCount();
            return View(count);
        }
    }
}
