import os

def prepare_path(path):
    return os.path.abspath(os.path.expanduser(path))

def open_file_secure(path):
  return os.open(
      path,
      os.O_CREAT | os.O_WRONLY | os.O_TRUNC,
      mode=0o600
  )

def create_directory_secure(directory_name):
    if not os.path.exists(directory_name):
        os.makedirs(directory_name, mode=0o770)

def open_path_secure(path):
    directory_name = os.path.dirname(path)

    create_directory_secure(directory_name)

    return open_file_secure(path)
