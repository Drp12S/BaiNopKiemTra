using System;
using System.Linq;
using System.Web.Mvc;
using Warehouse_25dh110314.Models;

namespace Warehouse_25dh110314.Controllers
{
    public class ProductsController : Controller
    {
        private Warehouse_25dh110314Entities db = new Warehouse_25dh110314Entities();

        public ActionResult Index()
        {
            var products = db.Products.ToList();
            return View(products);
        }

        public ActionResult Edit(int id)
        {
            var product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Product model)
        {
            var product = db.Products.Find(model.ProductID);
            if (product == null)
            {
                return HttpNotFound();
            }

            if (string.IsNullOrWhiteSpace(model.ProductName))
            {
                ModelState.AddModelError("ProductName", "Tên sản phẩm không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(model.Category))
            {
                ModelState.AddModelError("Category", "Danh mục không được để trống.");
            }

            if (model.Price <= 0)
            {
                ModelState.AddModelError("Price", "Giá mới phải lớn hơn 0.");
            }

            if (ModelState.IsValid)
            {
                product.ProductName = model.ProductName;
                product.Category = model.Category;
                product.Price = model.Price;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            model.Quantity = product.Quantity;
            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        public ActionResult Filter(decimal? minPrice, decimal? maxPrice, string category, string sortOrder)
        {
            var categories = db.Products.Select(p => p.Category).Distinct().ToList();
            ViewBag.Categories = categories;

            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;
            ViewBag.CurrentCategory = category;
            ViewBag.CurrentSortOrder = sortOrder;

            var query = db.Products.AsQueryable();

            if (minPrice.HasValue && maxPrice.HasValue && minPrice.Value > maxPrice.Value)
            {
                ViewBag.ErrorMessage = "Khoảng giá không hợp lệ (Từ giá phải nhỏ hơn hoặc bằng Đến giá).";
            }
            else
            {
                if (minPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= maxPrice.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category == category);
            }

            if (sortOrder == "asc")
            {
                query = query.OrderBy(p => p.Price);
            }
            else if (sortOrder == "desc")
            {
                query = query.OrderByDescending(p => p.Price);
            }

            var result = query.ToList();
            return View(result);
        }

        [HttpGet]
        public ActionResult AdjustPrice()
        {
            ViewBag.ProductList = new SelectList(db.Products.ToList(), "ProductID", "ProductName");
            ViewBag.Products = db.Products.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdjustPrice(int? productId, decimal? percentage, string adjustType)
        {
            ViewBag.ProductList = new SelectList(db.Products.ToList(), "ProductID", "ProductName", productId);

            if (!productId.HasValue || !percentage.HasValue || string.IsNullOrEmpty(adjustType))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ thông tin sản phẩm, tỷ lệ và thao tác.";
                ViewBag.Products = db.Products.ToList();
                return View();
            }

            var product = db.Products.Find(productId.Value);
            if (product == null)
            {
                ViewBag.ErrorMessage = "Không tìm thấy sản phẩm.";
                ViewBag.Products = db.Products.ToList();
                return View();
            }

            decimal rate = percentage.Value;

            if (adjustType == "increase")
            {
                if (rate <= 0 || rate > 100)
                {
                    ViewBag.ErrorMessage = "Tỷ lệ tăng giá phải lớn hơn 0% và không vượt quá 100%.";
                    ViewBag.Products = db.Products.ToList();
                    return View();
                }

                decimal newPrice = Math.Round(product.Price * (1 + (rate / 100m)), 2);
                product.Price = newPrice;
                db.SaveChanges();
                ViewBag.SuccessMessage = "Tăng giá thành công! Giá mới của " + product.ProductName + " là: " + newPrice.ToString("N0") + " đ";
            }
            else if (adjustType == "decrease")
            {
                if (rate <= 0 || rate >= 100)
                {
                    ViewBag.ErrorMessage = "Tỷ lệ giảm giá phải lớn hơn 0% và nhỏ hơn 100%.";
                    ViewBag.Products = db.Products.ToList();
                    return View();
                }

                decimal newPrice = Math.Round(product.Price * (1 - (rate / 100m)), 2);

                if (newPrice < 1000)
                {
                    ViewBag.ErrorMessage = "Từ chối: Giá sau khi giảm là " + newPrice.ToString("N0") + " đ (nhỏ hơn 1.000 đ). CSDL không đổi.";
                    ViewBag.Products = db.Products.ToList();
                    return View();
                }

                product.Price = newPrice;
                db.SaveChanges();
                ViewBag.SuccessMessage = "Giảm giá thành công! Giá mới của " + product.ProductName + " là: " + newPrice.ToString("N0") + " đ";
            }

            ViewBag.Products = db.Products.ToList();
            return View();
        }

        [HttpGet]
        public ActionResult BatchDiscount()
        {
            ViewBag.ProductList = new SelectList(db.Products.ToList(), "ProductID", "ProductName");
            ViewBag.Products = db.Products.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BatchDiscount(int? productAId, decimal? discountA, int? productBId, decimal? discountB)
        {
            ViewBag.ProductList = new SelectList(db.Products.ToList(), "ProductID", "ProductName");

            if (!productAId.HasValue || !discountA.HasValue || !productBId.HasValue || !discountB.HasValue)
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ thông tin cho cả hai sản phẩm.";
                ViewBag.Products = db.Products.ToList();
                return View();
            }

            if (productAId.Value == productBId.Value)
            {
                ViewBag.ErrorMessage = "Vui lòng chọn hai sản phẩm khác nhau.";
                ViewBag.Products = db.Products.ToList();
                return View();
            }

            var productA = db.Products.Find(productAId.Value);
            var productB = db.Products.Find(productBId.Value);

            if (productA == null || productB == null)
            {
                ViewBag.ErrorMessage = "Không tìm thấy thông tin sản phẩm.";
                ViewBag.Products = db.Products.ToList();
                return View();
            }

            if (discountA.Value <= 0 || discountA.Value >= 100 || discountB.Value <= 0 || discountB.Value >= 100)
            {
                ViewBag.ErrorMessage = "Tỷ lệ giảm của cả hai sản phẩm phải lớn hơn 0% và nhỏ hơn 100%.";
                ViewBag.Products = db.Products.ToList();
                return View();
            }

            decimal newPriceA = Math.Round(productA.Price * (1 - (discountA.Value / 100m)), 2);
            decimal newPriceB = Math.Round(productB.Price * (1 - (discountB.Value / 100m)), 2);

            if (newPriceA < 1000 || newPriceB < 1000)
            {
                ViewBag.ErrorMessage = "Từ chối toàn bộ: Có sản phẩm sau giảm giá nhỏ hơn 1.000 đ. Giao dịch bị hủy, giá cả hai sản phẩm giữ nguyên.";
                ViewBag.Products = db.Products.ToList();
                return View();
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    productA.Price = newPriceA;
                    productB.Price = newPriceB;

                    db.SaveChanges();
                    transaction.Commit();

                    ViewBag.SuccessMessage = "Giao dịch thành công! Đã cập nhật giá mới: " + productA.ProductName + " (" + newPriceA.ToString("N0") + " đ) và " + productB.ProductName + " (" + newPriceB.ToString("N0") + " đ).";
                }
                catch
                {
                    transaction.Rollback();
                    ViewBag.ErrorMessage = "Đã xảy ra lỗi hệ thống. Toàn bộ giao dịch đã được hoàn tác (Rollback).";
                }
            }

            ViewBag.Products = db.Products.ToList();
            return View();
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Product model)
        {
            if (string.IsNullOrWhiteSpace(model.ProductName))
            {
                ModelState.AddModelError("ProductName", "Tên sản phẩm không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(model.Category))
            {
                ModelState.AddModelError("Category", "Danh mục không được để trống.");
            }

            if (model.Price <= 0)
            {
                ModelState.AddModelError("Price", "Đơn giá phải lớn hơn 0.");
            }

            if (model.Quantity < 0)
            {
                ModelState.AddModelError("Quantity", "Số lượng phải là số nguyên không âm (từ 0 trở lên).");
            }

            if (ModelState.IsValid)
            {
                db.Products.Add(model);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(model);
        }

        public ActionResult Statistics()
        {
            int totalProducts = db.Products.Count();
            int highPriceProductsCount = db.Products.Count(p => p.Price >= 500000);
            decimal totalInventoryValue = db.Products.Sum(p => (decimal?)(p.Price * p.Quantity)) ?? 0;

            ViewBag.TotalProducts = totalProducts;
            ViewBag.HighPriceProductsCount = highPriceProductsCount;
            ViewBag.TotalInventoryValue = totalInventoryValue;

            var products = db.Products.ToList();
            return View(products);
        }
    }
}