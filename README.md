# Reactive.Testing.Marbles
### What is it?
Reactive.Testing.Marbles is a marble testing library inspired by [RxJS marble testing](https://github.com/ReactiveX/rxjs/blob/master/doc/marble-testing.md) and [MarbleTest.Net](https://github.com/alexvictoor/MarbleTest.Net). The goal of this library is to provide support for the RxJS marble syntax (except materialization of inner observables), while at the same time, adding new features that can make testing observables in easier for .NET developers.

### Getting started
If you have never done marble testing, it might be worth checking out some guides:

- [RxJS Marble Testing: RTFM](https://ncjamieson.com/marble-testing-rtfm/)
- [Introduction to RxJS Marble Testing (video)](https://egghead.io/lessons/rxjs-introduction-to-rxjs-marble-testing)

### Usage

If you got through the above resources then the following code should be self explanatory! Here's a quick example of how to do marble testing with C#.

```c#
var scheduler = new MarbleTestScheduler();

var source = scheduler.CreateHotObservable<char>("--^-a-b-c-|");
var subs = "^-------!";
var expected = "--b-c-d-|";

// This projects the sequence in a way that 'a' becomes 'b', 'b' becomes 'c' and so on.
var destination = source.Select(x => (char) (x + 1));

scheduler.ExpectObservable(destination).ToBe(expected);
scheduler.ExpectSubscriptions(source.Subscriptions).ToBe(subs);

scheduler.Flush();
```

You can also use attach specific values to each character (or marble), for example, the following test asserts that the sum of a cold sequence is 70:

```c#
var scheduler = new MarbleTestScheduler();

var source = scheduler.CreateColdObservable<int>("-a-b-c-|", new { a = 10, b = 20, c = 40 });
var expected = "-------(d|)";
var destination = source.Sum();

scheduler.ExpectObservable(destination).ToBe(expected, new { d = 70 });

scheduler.Flush();
``` 

### Marble Syntax

The RxJS repo has a [really good explanation of the syntax](https://github.com/ReactiveX/rxjs/blob/master/docs_app/content/guide/testing/marble-testing.md#marble-syntax) so please go and check it! It is a goal of this library to implement the full syntax and if you think something is missing, please raise and issue!

### Marble Specs

Much of the appeal of marble testing comes from being able to capture the temporal relationship of observables in a simple diagram. In other libraries such as RxJS and MarbleTest.Net that often requires the marbles and variables to have a extra spaces between declaration and assigment to make sure the marbles align. Automatic code formatting and linting can easily ruin the alignment and the clarity of the diagram is lost.

To counter this issue, Reactive.Testing.Marbles has the concept of marble specs. A marble spec allows you to define all the diagrams upfront and only later on make the decision on what hot/cold and what will be expected. Here's an example:

```c#
var scheduler = new MarbleTestScheduler();

var spec = new MarbleSpec(scheduler, @"
    e1  : ---a-------c----
    e2  : -------b-------d
    r   : ---a---b---c--
    sub : ^------------!
");

var e1 = spec.Cold<char>("e1");
var e2 = spec.Cold<char>("e2");
var r = e1.Merge(e2);

spec.ExpectObservable(r, "sub").ToBe("r");

scheduler.Flush();
```

### Testing Frameworks

Reactive.Testing.Marbles can be used with any testing framework. 

##### NUnit
A specific package is offered for NUnit that should help you avoid writing repetitive code. As an example, the previous example could be written in NUnit as:

```c#
[TestFixture]
public class MyTests : MarbleSpecReactiveTest
{
    [Test]
    public void Test()
    {
        Spec(@"
            e1  : ---a-------c----
            e2  : -------b-------d
            r   : ---a---b---c--
            sub : ^------------!
        ");

        var e1 = Cold<char>("e1");
        var e2 = Cold<char>("e2");
        var r = e1.Merge(e2);

        ExpectObservable(r, "sub").ToBe("r");
    }
}
```

##### Other frameworks
If you'd like to see other frameworks supported, please raise an issue and preferably, send a PR!
