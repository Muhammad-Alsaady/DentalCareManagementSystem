# People Management - Demo Application

## ? What's Been Created

A complete CRUD (Create, Read, Update, Delete) application for managing people with:

### **Features Implemented:**

#### 1. **Grid Functionality**
- ? **Sorting** - Click column headers to sort data
- ? **Filtering** - Filter data in each column header
- ? **Pagination** - Navigate through pages with customizable page sizes (5, 10, 15, 20 rows per page)
- ? **Search** - Built into the grid filtering system
- ? **Responsive Design** - Works on mobile and desktop

#### 2. **CRUD Operations**
- ? **View** - Grid displays all people with details
- ? **Create** - Add new person with form validation
- ? **Edit** - Update existing person details
- ? **Delete** - Remove person with confirmation page
- ? **Details** - View person details in popup modal

#### 3. **Shared Components Used**
- ? **_Alert.cshtml** - Success/Error messages using TempData
- ? **_ActionPopUp.cshtml** - Modal popup for viewing details
- ? **MvcGrid/_Grid.cshtml** - Grid component with all features
- ? **MvcGrid/_Pager.cshtml** - Pagination component

## ?? Files Created

### **Model**
- `MVCGrid\Models\Person.cs` - Person entity with properties

### **Controller**
- `MVCGrid\Controllers\PersonController.cs` - Full CRUD controller with:
  - Index (GET/POST) - Grid display
  - Create (GET/POST) - Add new person
  - Edit (GET/POST) - Update person
  - Delete (GET/POST) - Remove person
  - Details (GET) - View person in popup

### **Views**
- `MVCGrid\Views\Person\Index.cshtml` - Main grid view
- `MVCGrid\Views\Person\Create.cshtml` - Create form
- `MVCGrid\Views\Person\Edit.cshtml` - Edit form
- `MVCGrid\Views\Person\Delete.cshtml` - Delete confirmation
- `MVCGrid\Views\Person\_PersonDetails.cshtml` - Details partial (for popup)

## ?? How to Use

### **Access the Application**

1. Run the application:
   ```bash
   dotnet run --project MVCGrid\MVCGrid.csproj
   ```

2. Navigate to: **http://localhost:5xxx/Person**

3. Or click the **People** menu item in the navigation bar

### **Operations**

#### **View People**
- The grid automatically displays all people
- Click column headers to sort
- Type in filter boxes to search
- Use pagination controls at the bottom

#### **Add New Person**
1. Click **"Add New Person"** button (green, top-right)
2. Fill in the form:
   - First Name *
   - Last Name *
   - Email *
   - Phone *
   - Department * (dropdown)
   - Date of Birth *
   - Active (checkbox)
3. Click **"Create Person"**
4. Success message will appear

#### **Edit Person**
1. In the grid, click the **blue Edit icon** (pencil)
2. Update the form fields
3. Click **"Update Person"**
4. Success message will appear

#### **Delete Person**
1. In the grid, click the **red Delete icon** (trash)
2. Review person details
3. Confirm deletion
4. Success message will appear

#### **View Details**
1. In the grid, click the **blue View icon** (eye)
2. Details popup will appear
3. Can edit or delete directly from popup
4. Click "Close" or outside popup to dismiss

## ?? Features Showcase

### **Grid Features:**

#### Sorting
- Click any column header to sort ascending
- Click again to sort descending
- Sort order indicator shows in header

#### Filtering
- Header filters for each column
- Type to filter text columns
- Department and Status are filterable
- Real-time filtering as you type

#### Pagination
- Page size selector (5, 10, 15, 20 rows)
- First/Previous/Next/Last page buttons
- Page number buttons (shows 5 at a time)
- Total rows displayed

#### Actions Column
- **View** (blue eye icon) - Opens details popup
- **Edit** (blue pencil icon) - Go to edit page
- **Delete** (red trash icon) - Go to delete confirmation

### **Alerts:**

Success messages appear after:
- Creating a person
- Updating a person
- Deleting a person

Error messages appear when:
- Person not found
- Form validation fails
- Any operation error occurs

### **Status Badges:**
- **Active** - Green badge
- **Inactive** - Red badge

## ?? Sample Data

The application comes with 10 sample people:

| ID | Name | Email | Department | Status |
|----|------|-------|------------|--------|
| 1 | John Doe | john.doe@example.com | IT | Active |
| 2 | Jane Smith | jane.smith@example.com | HR | Active |
| 3 | Mike Johnson | mike.johnson@example.com | Finance | Active |
| 4 | Sarah Williams | sarah.williams@example.com | Marketing | Inactive |
| 5 | Robert Brown | robert.brown@example.com | IT | Active |
| 6 | Emily Davis | emily.davis@example.com | Sales | Active |
| 7 | David Miller | david.miller@example.com | IT | Active |
| 8 | Lisa Wilson | lisa.wilson@example.com | HR | Inactive |
| 9 | James Moore | james.moore@example.com | Finance | Active |
| 10 | Amanda Taylor | amanda.taylor@example.com | Marketing | Active |

## ?? Technical Details

### **Grid Configuration:**

```csharp
var grid = new Grid<Person>(people)
{
    Name = "PersonGrid",
    Id = "PersonGrid",
    EmptyText = "No people found",
    FilterMode = GridFilterMode.Header,
    Url = Url.Action("Index")
};
```

### **Grid Columns:**
1. **ID** - Numeric, filterable, sortable
2. **First Name** - Text, filterable, sortable
3. **Last Name** - Text, filterable, sortable
4. **Email** - Text, filterable, sortable
5. **Phone** - Text, filterable, sortable
6. **Department** - Text, filterable, sortable
7. **Date of Birth** - Date formatted (MM/dd/yyyy), sortable
8. **Status** - Badge (Active/Inactive), filterable, sortable
9. **Actions** - View/Edit/Delete buttons

### **Pager Configuration:**
```csharp
grid.Pager = new GridPager<Person>(grid)
{
    PartialViewName = "MvcGrid/_Pager",
    PageSizes = new Dictionary<int, string> { 
        { 5, "5" }, { 10, "10" }, { 15, "15" }, { 20, "20" } 
    },
    ShowPageSizes = true,
    RowsPerPage = 10,
    PagesToDisplay = 5
};
```

## ?? Next Steps

### **To Integrate with Database:**

1. Add Entity Framework Core DbContext
2. Replace in-memory list with database queries
3. Add proper async/await patterns
4. Add data validation attributes

Example:
```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<Person> People { get; set; }
}

// In Controller
private readonly ApplicationDbContext _context;

public async Task<IActionResult> Index()
{
    var people = await _context.People.ToListAsync();
    return View(CreateGrid(people));
}
```

### **To Add More Features:**

1. **Export to Excel/PDF** - Add export buttons
2. **Bulk Delete** - Add checkboxes for multi-select
3. **Advanced Search** - Add search form above grid
4. **Import from CSV** - Add upload functionality
5. **Audit Trail** - Track who created/modified records
6. **File Attachments** - Add document upload

## ?? Tips

- **Data persists in memory** - Restarts will reset data
- **Validation** - Client and server-side validation included
- **Responsive** - Test on mobile/tablet devices
- **Icons** - Font Awesome 6 icons used throughout
- **Bootstrap 5** - Modern, responsive design

## ? Summary

You now have a fully functional people management system with:
- ? Complete CRUD operations
- ? Grid with sorting, filtering, and pagination
- ? Success/Error alerts
- ? Modal popups for details
- ? Professional UI with Bootstrap 5
- ? Responsive design
- ? Form validation
- ? Ready to extend with database

Enjoy your MVC Grid application! ??
