# IL-instrumentation-based obvervables for C#

The idea is to implement something like MobX for .NET and to mirror it's API when it's possible and makes sense.

For now library supports:

- [observable properties](#observable-attribute)
- ObservableList<T>
- [Autorun](#autorun)
- [When](#when)
- [RunInAction](#runinaction)
- "@observer" **Blazor** components (see [usage with blazor](#usage-with-blazor))

Missing features (aka TODO):

- [computed properties](https://mobx.js.org/refguide/computed-decorator.html)
- [reaction](https://mobx.js.org/refguide/reaction.html)
- [@action decorator](https://mobx.js.org/refguide/action.html)


# Usage

## Installation

See also [Fody usage](https://github.com/Fody/Fody#usage).

### NuGet installation

Install the [NObservable NuGet package](https://nuget.org/packages/NObservable/)

```
dotnet add package NObservable
```


### Add to FodyWeavers.xml

Add `<NObservable/>` to [FodyWeavers.xml](https://github.com/Fody/Fody#add-fodyweaversxml)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Weavers>
  <NObservable/>
</Weavers>
```

## API

### [Observable] attribute

`[Observable]` instructs to instrument either one property or entire class. 
Property access will be tracked by NObservable.

Works like [@observable decorator](https://mobx.js.org/refguide/observable-decorator.html) from MobX but can be also applied to entire class

```cs
[Observable]
class Foo
{
    public int Prop1 { get; set; }
    public int Prop2 { get; set; }
}
```


```cs
class Bar
{
    [Observable]
    public int Foo { get; set; }
    public int NotTracked{ get; set; }
}

``` 


### Autorun

Works like [autorun](https://mobx.js.org/refguide/autorun.html) from MobX. 
It runs provided callback once and records all read property access to observable objects. 
If any of observed properties changes, callback will be run again and new property access list will be recorded


```cs

var o = new Foo{Prop1 = 1, Prop2 = 1};
Observe.Autorun(() => {
     Console.WriteLine($"Prop1: {o.Prop1}");
     if(o.Prop1 == 3)
        Console.WriteLine($"Prop1: {o.Prop1} Prop2: {o.Prop2}");
     else
        Console.WriteLine($"Prop1: {o.Prop1});    
});

o.Prop1 = 2;
o.Prop2 = 2;

o.Prop1 = 3;
o.Prop2 = 3;

```

Console output:

```
Prop1: 1
Prop1: 2
Prop1: 3, Prop2: 2
Prop1: 3, Prop2: 3
```


### When

Works like [when](https://mobx.js.org/refguide/autorun.html) from MobX. 
Either returns a task that completed when observed condition is met or runs a provided callback:

```cs

await Observe.When(() => o.Prop1 == 5);

Observe.When(() => o.Prop2 == 5, () => Console.WriteLine("callback"));

```

### RunInAction

Works like [runInAction](https://mobx.js.org/best/actions.html#the-runinaction-utility) from MobX.
Groups multiple property updates so change reactions won't be triggered on *each* prop.

Proper method instrumentation ([@action decorator](https://mobx.js.org/refguide/action.html) alternative)
aren't implemented **yet**, but unlike MobX it would be possible to make them properly work with `async` functions.

```cs
var o = new Foo{Prop1 = 1};
Observe.Autorun(() => Console.WriteLine(o.Prop1));
Observe.RunInAction(() => {
    o.Prop1++;
    o.Prop1++;
    o.Prop1 = 5;
    
});

```

Console output:
```
1
5
```


## Usage with Blazor

Install the [NObservable.Blazor NuGet package](https://nuget.org/packages/NObservable.Blazor/) 
and add [NObservable to FodyWeavers](#add-to-fodyweaversxml)

```
dotnet add package NObservable
```

Add UseNObservable to your `Startup.cs`:

```cs
public void Configure(IBlazorApplicationBuilder app)
{
    app.UseNObservable(); // <---------------
    app.AddComponent<App>("app");
}
```

Add `@using NObservable.Blazor` to `_ViewImports.cshtml`.


Add `@implements @implements IObserverComponent` at the top of your blazor component



Now your component should update if any observable properties used during the previous render are changed. 
Note, that NObservable adds its own `ShouldRender` implementation if your component doesn't have one already,
so automatic update after input events won't happen. 
To automatically update on local property changes decorate your local properties with `[Observable]`


### Some kind of example app

`AppModel.cs`:

```cs
[Observable]
public class AppModel
{
    public int Counter { get; set; }

    public AppModel()
    {
        Tick();
    }

    async void Tick()
    {
        while (true)
        {
            Counter++;
            await Task.Delay(1000);
        }
    }
}

```

`Startup.cs`:
```cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<AppModel>();
    }

    public void Configure(IBlazorApplicationBuilder app)
    {
        app.UseNObservable();
        app.AddComponent<App>("app");
    }
}
```

```cshtml
@page "/"
@implements IObserverComponent
@inject AppModel model

<h1>Counter demo</h1>


Counter: @(model.Counter)
```

Counter should tick automatically