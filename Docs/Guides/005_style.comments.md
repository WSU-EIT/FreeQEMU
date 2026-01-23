# FreeCRM: Comment Style Guide

> Voice and style conventions for code comments in FreeCRM-based projects.

---

## Document Index

| Section | Line | Description |
|---------|------|-------------|
| [Voice Characteristics](#voice-characteristics) | ~35 | Comment tone and style |
| [Core Comment Patterns](#core-comment-patterns) | ~55 | The 10 standard patterns |
| [XML Documentation](#xml-documentation) | ~200 | When and how to use XML docs |
| [Comment Formatting](#comment-formatting) | ~235 | Capitalization, punctuation, spacing |
| [What NOT to Comment](#what-not-to-comment) | ~280 | Anti-patterns to avoid |
| [Quick Reference Card](#quick-reference-card) | ~380 | Copy-paste pattern examples |

---

## Overview

**Purpose:** This guide establishes consistent comment patterns across all FreeCRM-based projects.

**Source:** Patterns derived from analysis of 500+ comments across multiple production projects.

**Key Principle:** Comments should read like "a calm, experienced developer walking through their thought process out loud."

---

## Voice Characteristics

The FreeCRM comment voice has these characteristics:

| Characteristic | Description |
|----------------|-------------|
| **Procedural** | Describes what the code does step-by-step |
| **Direct** | Gets to the point without preamble |
| **Present tense** | Uses active, present-tense verbs |
| **Instructional** | Reads like guiding the next developer |
| **Technical but plain** | Uses domain terms without jargon |
| **Impersonal** | No "I", "we", or "you" - just describes actions |

---

## Core Comment Patterns

### Pattern 1: Sequencing Comments

**Purpose:** Guide readers through multi-step operations using sequence words.

**When to use:** When code performs multiple related steps in order.

```csharp
// First, remove any existing photo
await dataAccess.DeleteUserPhoto(UserId);

// Now, save the new photo
await dataAccess.SaveFile(file);

// Next, update the user record
user.PhotoId = file.FileId;
```

**Sequence words to use:** `First,` `Now,` `Next,` `Then,` `Finally,`

---

### Pattern 2: Conditional Checks ("See if")

**Purpose:** Explain what condition is being checked.

**When to use:** Before if statements that check state or existence.

```csharp
// See if a TenantId is included in the header or querystring.
string tenantId = HeaderValue("TenantId");

// See if this tenant allows for creating new accounts automatically.
if (tenant.TenantSettings.AllowAutoCreateAccounts) {
    // ...
}

// See if the password uses the new hash format
if (PasswordHelper.IsNewHashFormat(password)) {
    // ...
}
```

**Note:** Both "See if" and "Make sure" patterns are valid and used interchangeably.

---

### Pattern 3: Validation Comments ("Make sure")

**Purpose:** Assert requirements or preconditions.

**When to use:** Before validation checks that guard against invalid state.

```csharp
// Make sure all required parameters are included. If not, just return the null object.
if (String.IsNullOrWhiteSpace(server)) return output;

// Make sure the user account is enabled
if (!user.Enabled) return output;

// Make sure the fingerprint matches
if (fingerprint != storedFingerprint) return output;
```

---

### Pattern 4: Conditional Logic ("If... then...")

**Purpose:** Explain branching logic clearly.

**When to use:** Before complex if statements with multiple conditions.

```csharp
// If the delete preference is to delete immediately, or if the item is already marked for
// delete, then delete now.
if (preference == DeletePreference.Immediate || item.Deleted) {
    // ...
}

// If there are any plugins to update user info, do that now.
await UpdateUserFromPlugins(user);
```

---

### Pattern 5: Context Comments ("This is")

**Purpose:** Explain what something represents or why it exists.

**When to use:** To clarify purpose of a code block or value.

```csharp
// This is a tenant-specific update. Send only to those people in that tenant group.
await Clients.Group(tenantId).SendAsync("update", data);

// This is a non-tenant-specific update.
await Clients.All.SendAsync("update", data);
```

---

### Pattern 6: File Header Comments

**Purpose:** Describe the purpose of app-specific extension files.

**When to use:** At the top of `.App.cs` partial class files.

```csharp
// Use this file as a place to put any application-specific API endpoints.

// Use this file as a place to put any application-specific data access methods.
```

---

### Pattern 7: Constraint Comments ("Only")

**Purpose:** Explain filtering or limiting behavior.

**When to use:** When code intentionally limits scope.

```csharp
// Only use the first result.
if (results.Count > 0) result = results.First();

// Only add if we don't already have this user
if (!existingUsers.Contains(user.UserId)) {
    existingUsers.Add(user);
}

// Only encrypt the value if it's not already encrypted.
if (!IsEncrypted(value)) value = Encrypt(value);
```

---

### Pattern 8: State Transition ("At this point")

**Purpose:** Mark when code reaches a significant state.

**When to use:** After validation passes or state changes.

```csharp
// At this point we are no longer under a lockout, so clear any previous lockouts.
user.LockoutCount = 0;

// At this point we can create the new request
var request = new DataObjects.Request();
```

---

### Pattern 9: Result State Comments

**Purpose:** Briefly describe the outcome state.

**When to use:** Before return statements or final assignments.

```csharp
// Valid login, so return the User Object
return user;

// Still locked out.
return output;
```

---

### Pattern 10: Action Comments

**Purpose:** Describe cleanup or modification operations.

**When to use:** Before removal, deletion, or modification code.

```csharp
// Remove any related records.
data.RelatedItems.RemoveRange(items);

// Remove sensitive info before returning
output.Password = null;

// Delete the folder and all media
await DeleteFolderContents(folderId);
```

---

## XML Documentation

### When to Use XML Docs

Use XML documentation for:
- Interface method signatures
- Public API methods
- Plugin entry points
- Complex return types

### XML Doc Style

```csharp
/// <summary>
/// Authenticates a user login
/// </summary>
/// <param name="EmailAddress">The username to authenticate</param>
/// <param name="Password">The password to authenticate</param>
/// <returns>True if the credentials are valid, otherwise returns false</returns>
public async Task<bool> Authenticate(string EmailAddress, string Password)
```

**Rules:**
- Sentence case for descriptions
- Start with lowercase for param/returns descriptions
- No period at end of single-line descriptions
- Use complete sentences for multi-line summaries

---

## Comment Formatting

### Capitalization

- Start with capital letter
- Use sentence case (not Title Case)
- Acronyms stay uppercase (GUID, LDAP, URL)

```csharp
// CORRECT
// Get the LDAP settings from the tenant

// AVOID
// Get The LDAP Settings From The Tenant
```

### Punctuation

- Single-line comments: period optional (often omitted)
- Multi-line comments: use periods
- Conditional phrases: period after complete thought

```csharp
// Single line - no period needed
// See if the record is deleted

// Multi-line - use periods
// The retry attempt has been exceeded. Mark this workflow as failed
// and notify the administrator.
```

### Spacing

- One space after `//`
- Blank line before comment blocks (when starting new logical section)
- No blank line between comment and code it describes

```csharp
// CORRECT
// See if a Token is included
string token = HeaderValue("Token");

// WRONG - missing space after //
//See if a Token is included
string token = HeaderValue("Token");
```

---

## What NOT to Comment

### Don't State the Obvious

```csharp
// AVOID - obvious from the code
// Increment counter
counter++;

// AVOID - obvious from the code
// Return the output
return output;
```

### Don't Use TODO Comments

If something needs work:
- Fix it now, or
- Create a task/issue in your tracking system

### Don't Use Humor or Emotion

```csharp
// AVOID
// This is a hack but it works!
// God knows why this is needed...

// CORRECT
// Required for backward compatibility with legacy tokens
```

### Don't Explain Language Features

```csharp
// AVOID - explaining foreach
// Using a foreach loop to iterate through the list
foreach (var item in items) {

// CORRECT - explain purpose
// Process each pending approval request
foreach (var item in items) {
```

---

## Commented-Out Code

### Acceptable Uses

**Example code in templates:**
```csharp
// Example:
// data.MyTable.RemoveRange(data.MyTable.Where(x => x.TenantId == TenantId));
// await data.SaveChangesAsync();
```

**Module markers:**
```csharp
// {{ModuleItemStart:Appointments}}
// ... module-specific code ...
// {{ModuleItemEnd:Appointments}}
```

### Not Acceptable

- Dead code that will never be used
- "Just in case" code preservation
- Version history (use git for that)

---

## Comment Categories by Location

### Constructor Comments
Focus on initialization logic:
```csharp
// See if a TenantId is included in the header or querystring.
// Set the CurrentUser to a new User object.
```

### Method Body Comments
Focus on steps and decisions:
```csharp
// First, remove any existing records.
// Now, add the new items.
```

### Data Access Comments
Focus on what and why:
```csharp
// A combination of results from local users and Active Directory
// Only add if we don't already have this user
```

---

## Quick Reference Card

| Pattern | Example |
|---------|---------|
| **Sequence** | `// First,` `// Now,` `// Next,` |
| **Check condition** | `// See if...` |
| **Validate** | `// Make sure...` |
| **Branch logic** | `// If the..., then...` |
| **Explain purpose** | `// This is a...` |
| **Constraint** | `// Only...` |
| **State transition** | `// At this point...` |
| **Result state** | `// Valid...` `// Still...` |
| **Action** | `// Remove...` `// Delete...` |
| **File purpose** | `// Use this file as a place to...` |

---

*Category: 005_style*
*Last Updated: 2025-12-23*
*Source: Analysis of 500+ comments across production FreeCRM projects*
