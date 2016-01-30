[![Build status](https://img.shields.io/teamcity/https/teamcity.bixbots.com/s/NCodeCompositionDisposableParts_Build.svg?label=TeamCity)](https://teamcity.bixbots.com/viewType.html?buildTypeId=NCodeCompositionDisposableParts_Build&guest=1)
[![Nuget](https://img.shields.io/nuget/dt/NCode.Composition.DisposableParts.svg)](https://www.nuget.org/packages/NCode.Composition.DisposableParts/)
[![Nuget](https://img.shields.io/nuget/v/NCode.Composition.DisposableParts.svg)](https://www.nuget.org/packages/NCode.Composition.DisposableParts/)


# NCode.Composition.DisposableParts
This library provides a fix for the IDisposable memory leak in Microsoft's Composition (MEF) framework.

## Problem
The Managed Extensibility Framework (MEF) from Microsoft has a known memory issue for any `IDisposable` type that is declared with the `NonShared` creation policy. In MEF, the `CompositionContainer` will hold a strong reference to every `IDisposable` type including types created with the `NonShared` creation policy for the entire lifetime of the container. This causes problems for non-shared types because those references won't be garbage collected until the container is disposed.

*References*

* [MEF keeps reference of NonShared IDisposable parts, not allowing them to be collected by GC][1]
* [Object lifetime of NonShared IDisposable parts][2]
* [MEF and memory][3]

[1]: http://stackoverflow.com/questions/8787982/mef-keeps-reference-of-nonshared-idisposable-parts-not-allowing-them-to-be-coll
[2]: http://mef.codeplex.com/discussions/285445
[3]: http://toreaurstad.blogspot.com/2012/09/freeing-up-memory-used-by-mef.html

## Solution
This library attempts to solve the problem by providing a custom catalog where the `GetExports` method returns a custom `ComposablePartDefinition` which doesn't create a disposable `ComposablePart` even if the underlying type is disposable. This prevents the `CatalogExportProvider` from storing a reference to the `ComposablePart` and allows the reference to be garbage collected at any time. Specifically this library will always *wrap* the part definition and then return a custom `ComposablePart` only if the underlying type implements `IDisposable` and specifies the `NonShared` creation policy.

## Usage
In order to use the library you must *wrap* the root `ComposablePartCatalog` with `DisposableWrapperCatalog` as such:
```
void SimpleExample()
{
  var container = new CompositionContainer(new DisposableWrapperCatalog(new ApplicationCatalog(), true));
  // ...
}

void MultipleCatalogs()
{
  var aggregate = new AggregateCatalog();
  var container = new CompositionContainer(new DisposableWrapperCatalog(aggregate, true));
  aggregate.Catalogs.Add(new ApplicationCatalog());
  aggregate.Catalogs.Add(new DirectoryCatalog("C:\\Foo\\Bar"));
  // ...
}
```
The `boolean` argument is whether to enable thread-safety or not.

## Feedback
Please provide any feedback, comments, or issues to this GitHub project [here][issues].

[issues]: https://github.com/NCodeGroup/NCode.Composition.DisposableParts/issues
