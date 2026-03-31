using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeesController : Controller
    {
        private readonly UserManager<Employee> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public EmployeesController(UserManager<Employee> userManager, RoleManager<IdentityRole> roleManager, AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: Employees
        public async Task<IActionResult> Index()
        {
            var employees = await _userManager.Users.ToListAsync();
            var employeeRoles = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var emp in employees)
            {
                var roles = await _userManager.GetRolesAsync(emp);
                employeeRoles[emp.Id] = roles.FirstOrDefault() ?? "None";
            }
            ViewBag.EmployeeRoles = employeeRoles;
            return View(employees);
        }

        // GET: Employees/Create
        public IActionResult Create()
        {
            // Lấy danh sách role từ RoleManager và chuyển thành SelectList
            var roles = _roleManager.Roles.Select(r => r.Name).ToList();
            ViewBag.Roles = new SelectList(roles);
            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee, string password, string role)
        {
            if (ModelState.IsValid)
            {
                // Tạo user
                employee.UserName = employee.Email;
                var result = await _userManager.CreateAsync(employee, password);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(role))
                        await _userManager.AddToRoleAsync(employee, role);
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
            // Nếu có lỗi, gán lại danh sách role
            var roleList = _roleManager.Roles.Select(r => r.Name).ToList();
            ViewBag.Roles = new SelectList(roleList);
            return View(employee);
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();
            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(employee);
            var currentRole = currentRoles.FirstOrDefault();

            var roles = _roleManager.Roles.Select(r => r.Name).ToList();
            ViewBag.Roles = new SelectList(roles, currentRole);
            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Employee employee, string role)
        {
            if (id != employee.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existing = await _userManager.FindByIdAsync(id);
                if (existing == null) return NotFound();

                existing.FullName = employee.FullName;
                existing.Address = employee.Address;
                existing.HireDate = employee.HireDate;
                existing.IsActive = employee.IsActive;
                existing.Email = employee.Email;
                existing.UserName = employee.Email;

                var result = await _userManager.UpdateAsync(existing);
                if (result.Succeeded)
                {
                    // Cập nhật role
                    var currentRoles = await _userManager.GetRolesAsync(existing);
                    await _userManager.RemoveFromRolesAsync(existing, currentRoles);
                    if (!string.IsNullOrEmpty(role))
                        await _userManager.AddToRoleAsync(existing, role);
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
            // Nếu lỗi, gán lại role list
            var roles = _roleManager.Roles.Select(r => r.Name).ToList();
            ViewBag.Roles = new SelectList(roles);
            return View(employee);
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();
            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null) return NotFound();
            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var employee = await _userManager.FindByIdAsync(id);
            if (employee != null)
                await _userManager.DeleteAsync(employee);
            return RedirectToAction(nameof(Index));
        }
    }
}