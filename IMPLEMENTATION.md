# TasnimBook administration and engagement extension

## Architecture

- `TasnimBook.Data` remains the shared EF Core entity/contract project.
- `webapicore` remains the only business-rule and persistence boundary.
- `TasnimBook.MAUI` continues to use its existing `AccountService`/navigation/MVVM approach.
- `TasnimBook.Admin` is a .NET 10 server-rendered MVC application. It keeps API credentials and JWTs server-side in an encrypted, HTTP-only, secure cookie rather than exposing them to browser JavaScript.

## Roles

| Capability | User | ContentAdmin | SuperAdmin |
|---|---:|---:|---:|
| Read/download/rate/like/library | Yes | Yes | Yes |
| Book/author/category CRUD and dashboard | No | Yes | Yes |
| Review admin applications or change admin state/role | No | No | Yes |

All administrative API endpoints enforce JWT role claims. Admin API keys remain an additional application credential, not an authorization substitute.

## Engagement and download policy

- Ratings, likes, library entries, and downloads use `(UserId, BookId)` composite keys.
- A user may update/delete one rating and toggle one like independently of favorites.
- A successful new local PDF download records a `Download` event; starting the reader records a `Read` event. Both upsert the personal-library relationship.
- Download analytics count the first successful download per authenticated user/book. Failures, retries, duplicate taps, and cached opens do not add rows.
- Removing a library entry keeps favorites, likes, downloaded files, and reading progress. A later read/download recreates the entry.
- Highest-rated dashboard results require at least three ratings. Popularity is `downloads × 3 + likes × 2 + ratings`.

## New API surface

- `api/Engagement/Book/{bookId}`
- `api/Engagement/Rating/{bookId}` (`PUT`, `DELETE`)
- `api/Engagement/Like/{bookId}/{liked}`
- `api/Engagement/LibraryEvent/{bookId}`
- `api/Engagement/Library` (`GET`, `DELETE /{bookId}`)
- `api/AdminAuth/Login`, `Register`, `Pending`, `Review/{id}`, `Administrators`, `SetActive`, `SetRole`
- `api/AdminContent/*` for paginated books and book/author/category CRUD
- `api/Dashboard/Overview`

## Database migration

`20261106010000_AddEngagementAndAdminRoles` adds role/approval fields to `appuser` and creates `bookrating`, `booklike`, `userlibrarybook`, and `bookdownload`. Existing accounts receive the safe `User` / `NotApplicable` defaults.

Apply migrations before starting the upgraded API:

```powershell
dotnet ef database update --project TasnimBook.Data --startup-project Api/webapicore
```

## Provisioning the first SuperAdmin

Do not use public registration. Set one-time environment variables (or user-secrets) on the API host, start the migrated API once, verify the account, then remove both settings:

```powershell
$env:BootstrapSuperAdmin__Email = "owner@example.com"
$env:BootstrapSuperAdmin__Password = "a-unique-14+-character-secret1"
dotnet run --project Api/webapicore
```

The provisioner runs only when no active SuperAdmin exists. The API also prevents demoting or deactivating the last active SuperAdmin.

Configure the admin application without committing secrets:

```powershell
dotnet user-secrets init --project TasnimBook.Admin
dotnet user-secrets set "Api:ApiKey" "<reader-api-key>" --project TasnimBook.Admin
dotnet user-secrets set "Api:BaseAddress" "https://your-api.example/api/" --project TasnimBook.Admin
```

## Known limitations

- The new admin editor accepts uploaded asset URLs and continues to preserve the legacy protected upload endpoint. A polished multipart upload/replace widget is not yet wired into the new admin UI.
- No database-backed integration test project existed in the repository. Build verification was completed, but authenticated end-to-end workflows require a migrated SQL Server test database and configured API keys.
- Existing legacy nullable warnings remain; this change does not attempt an unrelated domain-wide nullability refactor.
