#!/bin/bash

set -eu

BMX_CMD=${1-}
BMX_OPTS=""

if [[ $# > 0 ]]; then
  shift
  BMX_OPTS=$@

  for flag in username profile account role; do
    if [[ ! $* == *${flag}* ]]; then
      ENV="BMX_${flag^^}"

      if [[ ! -z ${!ENV-} ]]; then
        BMX_OPTS+=" --${flag} ${!ENV}"
      fi
    fi
  done
fi

bmx $BMX_CMD $BMX_OPTS