# Composition Layers Release Process

# Create a new release branch
```bash
git checkout -b 1.0.0-release
```

# Update Versions
Update the version in the `package.json` file
```json
"version": "1.0.0"
```

Update OpenXR version in the `installation.md` file
```markdown
[1.13.0](com.unity3d.kharma:upmpackage/com.unity.xr.openxr@1.13.0)
```

Update path in the External Surface Sample in `UploadAndroidFiles.cs`
```csharp
string sourceFolder = Path.Combine(Application.dataPath, "Samples/XR Composition Layers/1.0.0/Sample External Android Surface Project/StreamingAssets");
```

# Update Changelog
Update the `CHANGELOG.md` file with the new version and the changes made in this release. You should look at previous change log entries for formatting and examples.

Make sure to review all relevant commits and PRs since the last release to ensure that all changes are captured in the changelog. 

You can enter the following query in them github PR tab to search for all merged PRs between 2 dates:
``` markdown
is:pr is:merged merged:yyyy-mm-dd..yyyy-mm-dd 
```
or you can run the following command at the root of the OpenXR project to quickly check for merged PRs:
```bash
git log 1.0.0..HEAD --grep "Merge pull"
```


Example for `1.0.0`:
```markdown
## [1.0.0] - 2024-10-01
### Added
* Added new API

### Fixed
* Fixed crash

### Changed
* Removed Feature X
```

# Update License
Ensure that the `LICENSE.md` file is updated with the new year.

# Commit the Version and Changelog changes
```bash
git add -A
git commit -m "Changelog and version update for 1.0.0 release"
```

# Create a Release Pull Request
Create a PR from the release branch and name it using the version number (e.g. `Composition Layers 1.0.0 Release`).

Post the contents of the `CHANGELOG.md` file in the PR description.

Add reviewers as needed, generally this means adding the primary QA contact for Composition Layers as well as any developers that can help verify the changelog.

# Run Yamato Jobs
On the Release PR, confirm that the following jobs have passed:
- Pack
- Package Tests Trigger
- Test 2022.3 on win
- Validate 2022.3 on win
- check format any branch

These jobs should be automatically triggered when the PR is created.

# Test Samples
- Shape Sample Scene
    - Test general functionality


# Create the GitHub Release
Once the yamato jobs all pass the github release page can be created. This can be done [here](https://github.cds.internal.unity3d.com/unity/xr.sdk.compositionlayers/releases) by clicking the `Draft a new release` button.

The release is created to maintain a history of release binaries and changes.
- Change the branch to the release branch (e.g. `1.0.0-release`).
- Set the title to the version number (e.g. `1.0.0`).
- Set the tag to the version number (e.g. `1.0.0`).
- Paste the contents of the `CHANGELOG.md` file into the release description.
- Download the packages.zip from the `Pack` test artifacts and attach them to the release.
- Extract the `.tgz` files from the `packages.zip` and attach them to the release.
- Publish the release.

# Publish to the Internal Package Registry
Once QA has approved the release, the package can be published to the internal package registry. This can be done by running the `Publish to Internal Registry` [Yamato](https://unity-ci.cds.internal.unity3d.com/) job. It is important to note that once this step is done the version number for your release will be locked in and cannot be reused. This means if a bug is found further down the pipeline then the version number must be increased to make a new build.

You can confirm that the package has been published by checking the [internal package registry](https://artifactory.prd.cds.internal.unity3d.com/ui/packages/npm:%2F%2Fcom.unity.xr.compositionlayers?type=packages).

# Request Publishing to Production
After the release is created, the package must be promoted. This can be done by following this guide: [Promoting a Package](https://confluence.hq.unity3d.com/display/DEV/Promoting+a+Package)

For a reference, take a look at the [0.5.0](https://github.cds.internal.unity3d.com/unity/rm-package-promotion/pull/1333) release.

- Create a new PR on [rm-package-promotion](https://github.cds.internal.unity3d.com/unity/rm-package-promotion)
    - Update `version` under promotions/com.unity.xr.compositionlayers.yml
    - If needed, update `version` under promotions/com.unity.xr.openxr.yml

- Update and link the STAR checklist in the PR description
    - Head to the [Composition Layer STAR Checklist](https://star-checklist.ds.unity3d.com/Checklists/968/overview) and click on the *revalidate* button. Update this info as required. (For a more detailed guide on how to update the STAR checklist, refer to the [STAR Checklist Update Process for OpenXR](https://github.cds.internal.unity3d.com/unity/xr.sdk.openxr/blob/master/RELEASE.md#request-publish-to-production)).
    - Copy the link to the STAR checklist and paste it in the PR description.

- Once complete (and proper QA has been done), have the PR reviewed and approved by QA. This can be done by adding a comment with the following text:
    - `@qa approve`
- Once approved, allow the PR to be merged by the RM team.