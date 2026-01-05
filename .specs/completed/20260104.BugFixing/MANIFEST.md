# Bug Fixing Spec - January 4, 2026

**Source:** Beta Tester Feedback
**Priority:** High - These issues affect core user workflows

---

## Bug #1: Session Image Upload Fails

### Symptom
When uploading an image to a session, the user sees: **"Failed to process server response"**

### Root Cause Analysis
The iOS `APIClient.upload()` method attempts to decode the response directly as `ImageUploadResult`, but the backend returns a wrapped `APIResponse<ImageUploadResultDto>` structure.

**Backend returns:**
```json
{
  "success": true,
  "data": { "id": 1, "url": "...", "thumbnailUrl": "...", "originalFileName": "...", "displayOrder": 0 },
  "message": "Image uploaded successfully",
  "error": null
}
```

**iOS expects:** The raw `ImageUploadResult` object without the wrapper.

### Files to Modify

| File | Change |
|------|--------|
| `TrueDope-iOS/TrueDope-iOS/Core/Network/APIClient.swift` | Update `upload()` method to decode `APIResponse<T>` wrapper and extract the `data` field, consistent with other API methods |

### Fix Details
In `APIClient.swift`, the `upload()` method (around line 197) currently does:
```swift
let decoded = try self.decode(data, as: T.self)
```

Should be changed to decode `APIResponse<T>` and extract `data`:
```swift
let apiResponse = try self.decode(data, as: APIResponse<T>.self)
guard apiResponse.success else {
    throw APIError.serverError(statusCode: response.statusCode ?? 0, response: nil)
}
guard let data = apiResponse.data else {
    throw APIError.emptyResponse
}
return data
```

---

## Bug #2: Ammunition "Other" Caliber - Disappearing Row

### Symptom
1. User selects "Other" from the Caliber picker
2. A new row appears with a text field showing "Other"
3. When user tries to type a custom caliber (e.g., "6 ARC"), the text field disappears
4. Confusing UX where default text is "Other" which may lead users to think it will be saved as "Other"

### Root Cause Analysis
In [AmmunitionFormView.swift](TrueDope-iOS/TrueDope-iOS/Features/Gear/AmmunitionFormView.swift#L66-L75), the same `$caliber` state variable is used for both the Picker selection AND the TextField input:

```swift
Picker("Caliber", selection: $caliber) { ... }

if caliber == "Other" {
    TextField("Custom Caliber", text: $caliber)  // <-- Same binding!
}
```

When the user types in the TextField, it changes `caliber` from `"Other"` to their typed value. This makes `caliber == "Other"` false, causing the TextField to disappear mid-typing.

### Files to Modify

| File | Change |
|------|--------|
| `TrueDope-iOS/TrueDope-iOS/Features/Gear/AmmunitionFormView.swift` | Separate state variables for picker selection vs custom caliber text; add "6 ARC" to common calibers list |

### Fix Details

1. **Add separate state variable for custom caliber:**
```swift
@State private var caliber: String = ""
@State private var customCaliber: String = ""  // NEW
@State private var isCustomCaliber: Bool = false  // NEW - tracks if "Other" selected
```

2. **Update Picker to use computed selection:**
```swift
Picker("Caliber", selection: Binding(
    get: { isCustomCaliber ? "Other" : caliber },
    set: { newValue in
        if newValue == "Other" {
            isCustomCaliber = true
            customCaliber = ""
        } else {
            isCustomCaliber = false
            caliber = newValue
        }
    }
)) {
    Text("Select Caliber").tag("")
    ForEach(commonCalibers, id: \.self) { cal in
        Text(cal).tag(cal)
    }
}

if isCustomCaliber {
    TextField("Enter Custom Caliber", text: $customCaliber)
}
```

3. **Update save logic to use the correct caliber value:**
```swift
let finalCaliber = isCustomCaliber ? customCaliber : caliber
```

4. **Update `populateForm` to handle custom calibers when editing:**
```swift
if commonCalibers.contains(ammo.caliber) && ammo.caliber != "Other" {
    caliber = ammo.caliber
    isCustomCaliber = false
} else {
    isCustomCaliber = true
    customCaliber = ammo.caliber
}
```

5. **Add "6 ARC" to the common calibers list** (line 41-48):
```swift
private let commonCalibers = [
    ".223 Rem", ".224 Valkyrie", ".243 Win", ".260 Rem",
    "6mm ARC", "6mm Creedmoor", "6.5 Grendel", "6.5 Creedmoor", "6.5 PRC",  // Added 6mm ARC
    ".270 Win", ".277 Fury", ".284 Win", "7mm-08", "7mm Rem Mag",
    "7mm PRC", ".308 Win", ".30-06", ".300 Win Mag", ".300 PRC",
    ".300 Norma", ".338 Lapua", ".375 CheyTac", ".408 CheyTac",
    ".50 BMG", "9mm", ".45 ACP", "Other"
]
```

---

## Bug #3: Session Edit/Note Save Fails with "Server returned an empty response"

### Status: ✅ FIXED (January 4, 2026)

### Symptom
When editing a session (particularly adding/modifying notes), a popup appears: **"Failed to save: Server returned an empty response."**

### Root Cause Analysis
The backend `PUT /api/sessions/{id}` endpoint returns `ApiResponse.Ok()` with no data payload:

**Backend code in [SessionsController.cs](TrueDope/backend/src/TrueDope.Api/Controllers/SessionsController.cs):**
```csharp
return Ok(ApiResponse.Ok("Session updated successfully"));  // No data!
```

**Backend returns:**
```json
{
  "success": true,
  "data": null,
  "message": "Session updated successfully",
  "error": null
}
```

The iOS client in [SessionService.swift](TrueDope-iOS/TrueDope-iOS/Core/Services/SessionService.swift#L100-L104) expects `SessionDetail` data:
```swift
func updateSession(id: Int, _ request: UpdateSessionRequest) async throws -> SessionDetail {
    return try await apiClient.requestData(endpoint)  // Expects data!
}
```

When `APIClient.requestData()` receives a response with `data: null`, it throws `APIError.emptyResponse`.

### Files Modified

| File | Change |
|------|--------|
| `TrueDope/backend/src/TrueDope.Api/Services/ISessionService.cs` | Changed `UpdateSessionAsync` return type from `Task<bool>` to `Task<SessionDetailDto?>` |
| `TrueDope/backend/src/TrueDope.Api/Services/SessionService.cs` | Updated `UpdateSessionAsync` to return `null` on not found, and call `GetSessionAsync()` to return the full updated session after saving |
| `TrueDope/backend/src/TrueDope.Api/Controllers/SessionsController.cs` | Updated PUT endpoint to return `ApiResponse<SessionDetailDto>.Ok(updated)` with the full session data |

### Additional UX Enhancement

Also added "Save Details" button state tracking:

| File | Change |
|------|--------|
| `TrueDope-iOS/TrueDope-iOS/Features/Sessions/SessionEditViewModel.swift` | Added `hasUnsavedChanges` computed property that compares form values against original session |
| `TrueDope-iOS/TrueDope-iOS/Features/Sessions/SessionEditView.swift` | Button is now disabled and grayed out when no changes; enabled with accent color when changes exist |

---

## Implementation Order

1. **Bug #3** - Session save empty response (backend fix, highest impact) ✅ COMPLETE
2. **Bug #1** - Image upload response parsing (iOS fix)
3. **Bug #2** - Ammunition caliber UX (iOS fix, includes 6 ARC addition)

---

## Testing Checklist

### Bug #1 - Image Upload
- [ ] Upload a new image to a session
- [ ] Verify image appears in session detail
- [ ] Upload multiple images in sequence
- [ ] Verify proper error handling for failed uploads (network error, invalid file)

### Bug #2 - Ammunition Caliber
- [ ] Create new ammunition with a standard caliber (e.g., "6.5 Creedmoor")
- [ ] Create new ammunition with "Other" and enter custom caliber
- [ ] Edit existing ammunition - verify caliber displays correctly
- [ ] Edit ammunition with custom caliber - verify text field persists
- [ ] Verify "6 ARC" / "6mm ARC" appears in caliber picker list
- [ ] Verify custom caliber text field does NOT disappear while typing

### Bug #3 - Session Save
- [x] Edit session notes and save
- [x] Edit session date and save
- [x] Edit session conditions and save
- [x] Verify no "empty response" errors
- [x] Verify updated data reflects correctly after save
- [x] Verify Save button is grayed out when no changes
- [x] Verify Save button is blue/enabled when changes exist
