## 0.1.4
* Bump Hopac dependency to 0.3.22

## 0.1.3
* Added utility and timing extensions for `Promise`, `Job`, and `Alt`
* Added `OptionJob` extensions including `traverse` and `sequence`
* Added `ChoiceJob` extensions including `traverse` and `sequence`
* Added `UnixTime` implementation for getting the current time in Unix seconds since the epoch
* Added `Job.supervise` and `Job.superviseWithWill`
* Added automatic retry and exponential backoff policies

## 0.1.2
* `SharedMap.freeze` again returns a `Job<Map<'k,'v>>`

## 0.1.1
* Add serialized access map
* Internal refactoring
* `SharedMap.freeze` now returns an `Alt<Map<'k,'v>>`

## 0.1.0 - Initial Release
* Includes basic shared map
