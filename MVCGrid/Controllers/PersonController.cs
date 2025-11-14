using Microsoft.AspNetCore.Mvc;
using MVCGrid.Models;

namespace MVCGrid.Controllers
{
    public class PersonController : Controller
    {
        // In-memory data store (replace with database in production)
        private static List<Person> _people = new List<Person>
        {
            new Person { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Phone = "555-0001", Department = "IT", DateOfBirth = new DateTime(1985, 5, 15), IsActive = true },
            new Person { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com", Phone = "555-0002", Department = "HR", DateOfBirth = new DateTime(1990, 8, 22), IsActive = true },
            new Person { Id = 3, FirstName = "Mike", LastName = "Johnson", Email = "mike.johnson@example.com", Phone = "555-0003", Department = "Finance", DateOfBirth = new DateTime(1988, 3, 10), IsActive = true },
            new Person { Id = 4, FirstName = "Sarah", LastName = "Williams", Email = "sarah.williams@example.com", Phone = "555-0004", Department = "Marketing", DateOfBirth = new DateTime(1992, 11, 5), IsActive = false },
            new Person { Id = 5, FirstName = "Robert", LastName = "Brown", Email = "robert.brown@example.com", Phone = "555-0005", Department = "IT", DateOfBirth = new DateTime(1987, 7, 18), IsActive = true },
            new Person { Id = 6, FirstName = "Emily", LastName = "Davis", Email = "emily.davis@example.com", Phone = "555-0006", Department = "Sales", DateOfBirth = new DateTime(1991, 2, 28), IsActive = true },
            new Person { Id = 7, FirstName = "David", LastName = "Miller", Email = "david.miller@example.com", Phone = "555-0007", Department = "IT", DateOfBirth = new DateTime(1986, 9, 12), IsActive = true },
            new Person { Id = 8, FirstName = "Lisa", LastName = "Wilson", Email = "lisa.wilson@example.com", Phone = "555-0008", Department = "HR", DateOfBirth = new DateTime(1989, 4, 20), IsActive = false },
            new Person { Id = 9, FirstName = "James", LastName = "Moore", Email = "james.moore@example.com", Phone = "555-0009", Department = "Finance", DateOfBirth = new DateTime(1984, 12, 8), IsActive = true },
            new Person { Id = 10, FirstName = "Amanda", LastName = "Taylor", Email = "amanda.taylor@example.com", Phone = "555-0010", Department = "Marketing", DateOfBirth = new DateTime(1993, 6, 14), IsActive = true },
        };

        // GET: Person/Index
        public IActionResult Index()
        {
            return View(_people);
        }

        // GET: Person/GetGridData - Returns grid partial for AJAX
        public IActionResult GetGridData()
        {
            return PartialView("_PersonGrid", _people);
        }

        // GET: Person/Create - Returns create form partial
        public IActionResult Create()
        {
            return PartialView("_CreatePartial", new Person { IsActive = true });
        }

        // POST: Person/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Person person)
        {
            if (ModelState.IsValid)
            {
                person.Id = _people.Any() ? _people.Max(p => p.Id) + 1 : 1;
                _people.Add(person);
                
                return Json(new { 
                    success = true, 
                    message = $"Person '{person.FullName}' has been created successfully!" 
                });
            }

            return PartialView("_CreatePartial", person);
        }

        // GET: Person/Edit/5 - Returns edit form partial
        public IActionResult Edit(int id)
        {
            var person = _people.FirstOrDefault(p => p.Id == id);
            if (person == null)
            {
                return Json(new { success = false, message = "Person not found." });
            }

            return PartialView("_EditPartial", person);
        }

        // POST: Person/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Person person)
        {
            if (id != person.Id)
            {
                return Json(new { success = false, message = "Invalid person ID." });
            }

            if (ModelState.IsValid)
            {
                var existingPerson = _people.FirstOrDefault(p => p.Id == id);
                if (existingPerson != null)
                {
                    existingPerson.FirstName = person.FirstName;
                    existingPerson.LastName = person.LastName;
                    existingPerson.Email = person.Email;
                    existingPerson.Phone = person.Phone;
                    existingPerson.Department = person.Department;
                    existingPerson.DateOfBirth = person.DateOfBirth;
                    existingPerson.IsActive = person.IsActive;

                    return Json(new { 
                        success = true, 
                        message = $"Person '{person.FullName}' has been updated successfully!" 
                    });
                }

                return Json(new { success = false, message = "Person not found." });
            }

            return PartialView("_EditPartial", person);
        }

        // GET: Person/Delete/5 - Returns delete confirmation partial
        public IActionResult Delete(int id)
        {
            var person = _people.FirstOrDefault(p => p.Id == id);
            if (person == null)
            {
                return Json(new { success = false, message = "Person not found." });
            }

            return PartialView("_DeletePartial", person);
        }

        // POST: Person/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var person = _people.FirstOrDefault(p => p.Id == id);
            if (person != null)
            {
                _people.Remove(person);
                return Json(new { 
                    success = true, 
                    message = $"Person '{person.FullName}' has been deleted successfully!" 
                });
            }

            return Json(new { success = false, message = "Person not found." });
        }

        // GET: Person/Details/5 - Returns details partial
        public IActionResult Details(int id)
        {
            var person = _people.FirstOrDefault(p => p.Id == id);
            if (person == null)
            {
                return Json(new { success = false, message = "Person not found." });
            }

            return PartialView("_DetailsPartial", person);
        }
    }
}
