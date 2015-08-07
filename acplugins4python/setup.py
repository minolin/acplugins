from setuptools import setup

plugin_version = "0.1"
ac_version = "1.2.2"

setup(name='acplugins4python',
      version=plugin_version,
      description='Python api layer for Assetto Corsa\'s UDP server API. Developed and tested with AC %s' % ac_version,
      url='https://github.com/minolin/acplugins',
      author='NeverEatYellowSnow (NEYS)',
      author_email='never_eat_yellow_snow@gmx.net',
      license='BSD',
      packages=['acplugins4python'], # put the three files (plus an empty __init__.py) into a folder "acserver"
      zip_safe=False,
      keywords='Assetto Corsa AC server')