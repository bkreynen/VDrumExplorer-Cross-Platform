# VDrumExplorer.Proto

This project is part of the VDrumExplorer, originally authored by
[Jon Skeet](https://github.com/jskeet) as part of his
[DemoCode](https://github.com/jskeet/DemoCode/tree/master/Drums)
repository. It is licensed under the Apache License 2.0.

---

To generate the code, run:

```sh
$PROTOC -I $PROTOROOT -I . DrumFiles.proto  --csharp_out=. --csharp_opt=internal_access,file_extension=.g.cs
```

... with a suitable value for PROTOC and PROTOROOT.
