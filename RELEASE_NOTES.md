### 0.1.3-beta001
* Added utility and timing extensions for `Promise`, `Job`, and `Alt`
* Added `OptionJob` extensions
* Added `ChoiceJob` extensions
* Added `UnixTime` implementation for getting the current time in Unix seconds since the epoch

### 0.1.2
* `SharedMap.freeze` again returns a `Job<Map<'k,'v>>`

### 0.1.1
* Add serialized access map
* Internal refactoring
* `SharedMap.freeze` now returns an `Alt<Map<'k,'v>>`

### 0.1.0 - Initial Release
* Includes basic shared map
