from setuptools import setup, find_packages

print('foo')
print(find_packages())
print('bar')

setup(
    name='bmx',
    version='0.0.1',
    description='An AWS CLI wrapper that renews STS tokens',
    url='https://github.com/Brightspace/bmx',
    license='Proprietary',
    classifiers = [
	'Development Status :: 3 - Alpha',

    	'Intended Audience :: Developers',
    	'Topic :: Security',

	'License :: Other/Proprietary License'
    ],
    keywords='aws cli okta sts token credentials',
    packages=['bmx'],
    entry_points={
        'console_scripts': [
            'bmx=bmx.bmx:main',
            'bmx-aws=bmx.bmxaws:main',
            'bmx-renew=bmx.bmxrenew:main'
        ]
    },
    install_requires=[
        'boto3>=1.4.5',
        'future>=0.16.0',
        'lxml>=3.8.0',
        'okta>=0.0.4',
        'pies2overrides>=2.6.7',
        'prompt>=0.4.1',
        'requests>=2.18.3',
        'setuptools>=20.7.0'
    ],
    author='Chris Redekop',
    author_email='chris.redekop@d2l.com'
)

