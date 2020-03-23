# Samples
Home of samples.

## Hooks
In the [Hooks](./Hooks) sample application, we use the `OnDeleteTransactor` and `PreCommitTransactor` extension types to extend `Starcounter.Database` to support callbacks when database objects are manipulated.

See [Program.cs](./Hooks/Program.cs) to see how it looks, or run the sample to see it in action:

```
cd Hooks
dotnet run
```

## NestedTransactions
The [NestedTransactions](./NestedTransactions) sample show how the `NestedTransactor` can be used to extend `Starcounter.Database` to support _nested transactions_, something not natively supported.

See [Program.cs](./NestedTransactions/Program.cs) to see how it looks, or run the sample to see it in action:

```
cd NestedTransactions
dotnet run
```