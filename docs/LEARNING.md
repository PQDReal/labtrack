# Learning walkthrough

## The concepts you can demonstrate

- C# records hold the sample's fields. A class groups persistence behavior.
- A WinForms event runs when the user clicks a button or changes text.
- `selectedId == 0` means a new sample; an existing ID means an update.
- `using` disposes database connections and commands even when an exception occurs.
- SQL parameters keep input separate from SQL syntax. They also handle apostrophes safely.
- `ArgumentException` reports an invalid field; the UI displays the message without crashing.
- SQLite stores rows on disk; closing and reopening the app preserves them.
- A test uses its own temporary database, so it cannot delete the user's real records.
- A self-contained publish includes the .NET runtime. Windows Forms still makes this a Windows app.

## Hands-on practice before the interview

1. Run the app and create three synthetic samples. Explain why a duplicate code is rejected.
2. Put a breakpoint in `SaveSample`. Step into `SampleStore.Save` and watch how the SQL parameters are filled.
3. Update a sample's status. Explain how the ID prevents a second row from being inserted.
4. Restart the app. Find the per-user database path in Program.cs.
5. Export notes containing commas and quotes. Find the CSV escaping code.
6. Run the tests. Temporarily remove a validation check, observe the relevant test failing, then restore it.
7. Make one small change yourself, such as renaming a field label. Rebuild and package.

## Honest interview framing

“This is a recent AI-assisted learning project using C#, .NET 8, WinForms and SQLite. I can walk through the event handlers, parameterized CRUD operations, validation and tests. It is a small local desktop application, not production biotech experience.”

Do not claim independent implementation, production deployment, or professional WinForms experience based solely on generated code. The repository is a concrete starting point for hands-on learning.
