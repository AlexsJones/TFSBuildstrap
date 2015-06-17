# TFSBuildstrap

Builds those irritating TFS build definitions that seem unbuildable on TeamCity.

###Usage
```
TFSBuildstrap.exe http://mytfs.8080/ DefaultProjectFoo FooConfiguration
```

###Example workflow with teamcity

```
git clone this repo
```

```
build step would to compile (VS2013) x64, Debug/Release
```

```
Next step would be to run the TFSBuildstrap.exe with your arguments [URL] [PROJECT] [DEFINITION]
```

Sit back and let teamcity chug away for a bit whilst it runs the definition.


####Caveats

It goes fairly unresponsive in the Teamcity build log as it's really just kicking off the queued build and a monitor process.
TFS build is doing all the work here, but it saves having to take a massive build definition and convert it to TC steps.
