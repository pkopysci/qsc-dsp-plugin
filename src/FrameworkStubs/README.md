# FrameworkStubs

Spec-faithful, compile-only stand-in for the three private GCU AV Framework
NuGet packages used by `QscDspDevices`:

- `gcu-common-utils` 4.3.3
- `gcu-hardware-service` 4.3.4
- `gcu-domain-service` 4.2.3

The stubs mirror every public type, member, and namespace documented in
`framework-docs/` verbatim. Most non-trivial member bodies throw
`NotImplementedException`. The two genuinely-trivial helpers
(`ParameterValidator`, `DataFormatter`) are implemented per spec because
their full behaviour fits in a few lines and is part of the public
contract.

## Why this project exists

The real `.nupkg` binaries are private. To compile and unit-test
`QscDspDevices` in isolation, we ship this stub assembly alongside the
production project. At delivery time the stub is swapped for the real
packages with no source change in the consumer — the public API surface
is identical by construction.

## Swap procedure (delivery)

1. Open `src/QscDspDevices/QscDspDevices.csproj`.
2. Replace this `<ProjectReference>` block:

   ```xml
   <ItemGroup>
     <ProjectReference Include="..\FrameworkStubs\FrameworkStubs.csproj" />
   </ItemGroup>
   ```

   with three real `<PackageReference>` lines:

   ```xml
   <ItemGroup>
     <PackageReference Include="gcu-common-utils"     Version="4.3.3" />
     <PackageReference Include="gcu-hardware-service" Version="4.3.4" />
     <PackageReference Include="gcu-domain-service"   Version="4.2.3" />
   </ItemGroup>
   ```

   Add matching `<PackageVersion>` entries to `Directory.Packages.props`
   if Central Package Management is in use.

3. From the repo root, restore and rebuild:

   ```bash
   dotnet restore
   dotnet build QscDspDevices.sln
   ```

4. The shipped `.nupkg` (`dotnet pack`) will reference the real GCU
   packages transitively and will not contain `FrameworkStubs.dll`.

## Keeping FrameworkStubs in the repo after delivery

The `FrameworkStubs` project should remain in the repository even after
the production swap. It stays unreferenced by the shipped library, but is
useful for:

- Re-running the unit / integration test suite in environments without
  the private GCU NuGet feed configured.
- Diff-auditing the documented API surface against future framework
  releases.

Because `FrameworkStubs.csproj` sets `<IsPackable>false</IsPackable>`,
the assembly is never published to a feed.
